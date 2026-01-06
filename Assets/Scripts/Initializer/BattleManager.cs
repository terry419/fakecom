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

    // 토큰 관리
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

        // 3. 마지막 상태 종료 시도
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

            var battleContext = new BattleContext(mapManager, turnManager, _uiManagerMock);
            _stateFactory = new BattleStateFactory(battleContext);

            BootManager.OnBootComplete += OnSystemBootFinished;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] 초기화 실패: {ex.Message}");
            CurrentStateID = BattleState.Error;
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

    public void HandleTransitionRequest(BattleState nextStateID, StatePayload payload)
    {
        TransitionAsync(nextStateID, payload).Forget();
    }

    // [중요 수정] 안전한 비동기 전환 로직
    private async UniTaskVoid TransitionAsync(BattleState nextStateID, StatePayload payload)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[BattleManager] 전환 중 중복 요청 무시됨: {nextStateID}");
            return;
        }
        _isTransitioning = true;

        // 1. 토큰 교체 (Rotation)
        var oldCts = _stateCts;
        _stateCts = new CancellationTokenSource();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyCts.Token, _stateCts.Token);

        try
        {
            // 2. 이전 상태 취소 (Dispose는 아직 하지 않음!)
            if (oldCts != null)
            {
                oldCts.Cancel();
            }

            if (_currentLogicState != null)
            {
                _currentLogicState.OnRequestTransition -= HandleTransitionRequest;
                // Exit 실행 (취소된 oldCts의 영향을 받지 않도록 None 전달)
                await _currentLogicState.Exit(CancellationToken.None);
            }

            // 3. [핵심] Exit이 완전히 끝난 후 안전하게 폐기
            if (oldCts != null)
            {
                oldCts.Dispose();
            }

            // 4. 다음 상태 생성 및 진입
            var oldID = CurrentStateID;
            _currentLogicState = _stateFactory.GetOrCreate(nextStateID);
            CurrentStateID = _currentLogicState.StateID;

            OnStateChanged?.Invoke(oldID, CurrentStateID);

            _currentLogicState.OnRequestTransition += HandleTransitionRequest;
            await _currentLogicState.Enter(payload, linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[BattleManager] 상태 전환 중 취소됨.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] 상태 전환 중 오류: {ex.Message}");
            _isTransitioning = false;

            CurrentStateID = BattleState.Error;
            _currentLogicState = _stateFactory.GetOrCreate(BattleState.Error);

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