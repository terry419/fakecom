using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

// FSM의 총괄 관리자
public class BattleManager : MonoBehaviour, IInitializable
{
    public BattleState CurrentStateID { get; private set; } = BattleState.None;
    public event Action<BattleState, BattleState> OnStateChanged;

    private BattleStateBase _currentLogicState;
    private BattleStateFactory _stateFactory;

    // [수정 1] 토큰 관리 분리
    // _destroyCts: 매니저 자체가 파괴될 때 취소 (OnDestroy용)
    // _stateCts: 상태가 전환될 때마다 이전 상태를 취소하고 새로 생성 (State용)
    private CancellationTokenSource _destroyCts;
    private CancellationTokenSource _stateCts;

    private bool _isTransitioning = false;

    // (임시) Mock UI
    private class UIManagerMock : IUIManager
    {
        public event Action OnStartButtonClick;
        public void ShowStartButton() => Debug.Log("[UIManagerMock] 전투 시작 버튼 표시");
        public void HideStartButton() => Debug.Log("[UIManagerMock] 전투 시작 버튼 숨김");
        public void AutoTriggerStartConfirmation()
        {
            UniTask.Delay(1500).ContinueWith(() => OnStartButtonClick?.Invoke()).Forget();
        }
    }
    private UIManagerMock _uiManagerMock;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
        _destroyCts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        // 1. 매니저 파괴 토큰 취소
        if (_destroyCts != null)
        {
            _destroyCts.Cancel();
            _destroyCts.Dispose();
        }

        // 2. 현재 상태 토큰 취소
        if (_stateCts != null)
        {
            _stateCts.Cancel();
            _stateCts.Dispose();
        }

        // 3. 마지막 상태 종료 시도 (결과는 보장 못함)
        _currentLogicState?.Exit(CancellationToken.None).Forget();

        ServiceLocator.Unregister<BattleManager>(ManagerScope.Scene);
        BootManager.OnBootComplete -= OnSystemBootFinished;
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            if (!ServiceLocator.TryGet(out MapManager mapManager))
                throw new Exception("MapManager가 등록되지 않았습니다!");

            if (!ServiceLocator.TryGet(out TurnManager turnManager))
                throw new Exception("TurnManager가 등록되지 않았습니다!");

            _uiManagerMock = new UIManagerMock();

            // Factory 및 Context 생성 로직 유지
            var battleContext = new BattleContext(mapManager, turnManager, _uiManagerMock);
            _stateFactory = new BattleStateFactory(battleContext);

            BootManager.OnBootComplete += OnSystemBootFinished;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] 초기화 실패: {ex.Message}");
            CurrentStateID = BattleState.Error;
            // 초기화 실패 시에는 별도 토큰 처리 없이 즉시 진입
            _stateCts = new CancellationTokenSource();
            _currentLogicState = new ErrorState(null);
            _currentLogicState.Enter(new ErrorPayload(ex), _stateCts.Token).Forget();
        }
        await UniTask.CompletedTask;
    }

    private void OnSystemBootFinished(bool isSuccess)
    {
        if (CurrentStateID == BattleState.Error) return;

        if (!isSuccess || _stateFactory == null)
        {
            HandleTransitionRequest(BattleState.Error, new ErrorPayload(new Exception("Boot failed or StateFactory not ready.")));
            return;
        }

        Debug.Log("[BattleManager] 시스템 부팅 완료. Setup 상태 진입 요청.");
        _uiManagerMock.AutoTriggerStartConfirmation();
        HandleTransitionRequest(BattleState.Setup, null);
    }

    // [수정 2] 외부 요청 처리
    public void HandleTransitionRequest(BattleState nextStateID, StatePayload payload)
    {
        // UniTaskVoid 메서드 호출
        TransitionAsync(nextStateID, payload).Forget();
    }

    // [수정 3] 안전한 비동기 전환 로직 (토큰 교체 적용)
    private async UniTaskVoid TransitionAsync(BattleState nextStateID, StatePayload payload)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[BattleManager] 전환 중 중복 요청 무시됨: {nextStateID}");
            return;
        }
        _isTransitioning = true;

        // 1. 기존 상태 토큰 정리 (Rotation)
        // 기존 토큰을 지역 변수에 백업하고, 멤버 변수는 즉시 새 토큰으로 교체
        // 이렇게 해야 Dispose된 토큰을 참조하는 Race Condition을 방지할 수 있음
        var oldCts = _stateCts;
        _stateCts = new CancellationTokenSource();

        // 두 토큰(매니저 파괴, 상태 전환)을 합친 LinkedToken 생성
        // 상태 내부에서는 이 토큰 하나만 체크하면 됨
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyCts.Token, _stateCts.Token);

        try
        {
            // 2. 이전 상태 취소 및 종료
            if (oldCts != null)
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            if (_currentLogicState != null)
            {
                _currentLogicState.OnRequestTransition -= HandleTransitionRequest;
                // Exit은 취소되지 않도록 배려하거나, 필요시 CancellationToken.None 사용
                await _currentLogicState.Exit(CancellationToken.None);
            }

            // 3. 다음 상태 생성 및 진입
            var oldID = CurrentStateID;
            _currentLogicState = _stateFactory.GetOrCreate(nextStateID);
            CurrentStateID = _currentLogicState.StateID;

            OnStateChanged?.Invoke(oldID, CurrentStateID);

            _currentLogicState.OnRequestTransition += HandleTransitionRequest;

            // 새 상태 진입 (LinkedToken 사용)
            await _currentLogicState.Enter(payload, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[BattleManager] 상태 전환 중 취소됨.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] 상태 전환 중 치명적 오류 발생. ErrorState로 전환.");

            // 에러 발생 시 상태 강제 전환 (재귀 호출 방지 위해 직접 처리)
            _isTransitioning = false; // 플래그 초기화
            CurrentStateID = BattleState.Error;
            _currentLogicState = _stateFactory.GetOrCreate(BattleState.Error);

            // 에러 상태용 새 토큰
            if (_stateCts != null) _stateCts.Dispose();
            _stateCts = new CancellationTokenSource();

            _currentLogicState.Enter(new ErrorPayload(ex), _stateCts.Token).Forget();
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private void Update()
    {
        if (_isTransitioning || _currentLogicState == null) return;

        try
        {
            _currentLogicState.Update();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] Update Error: {ex.Message}");
            HandleTransitionRequest(BattleState.Error, new ErrorPayload(ex));
        }
    }
}