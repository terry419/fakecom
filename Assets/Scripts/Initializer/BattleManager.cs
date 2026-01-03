using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [Scene Scope] 개별 전투의 흐름(FSM)을 총괄하는 관리자
public class BattleManager : MonoBehaviour, IInitializable
{
    public BattleState CurrentStateID { get; private set; } = BattleState.None;
    public event Action<BattleState, BattleState> OnStateChanged;

    private BattleStateBase _currentLogicState;
    private BattleStateFactory _stateFactory;
    private CancellationTokenSource _cts;
    private bool _isTransitioning = false;

    // (임시) 실제 UIManager 구현 전까지 사용할 Mock 객체
    private class UIManagerMock : IUIManager
    {
        public event Action OnStartButtonClick;
        public void ShowStartButton() => Debug.Log("[UIManagerMock] 전투 시작 버튼 표시");
        public void HideStartButton() => Debug.Log("[UIManagerMock] 전투 시작 버튼 숨김");

        // SetupState의 자동 진행을 위해 1.5초 뒤 강제로 이벤트 호출 (테스트용)
        public void AutoTriggerStartConfirmation()
        {
            UniTask.Delay(1500).ContinueWith(() => OnStartButtonClick?.Invoke()).Forget();
        }
    }
    private UIManagerMock _uiManagerMock;

    private void Awake()
    {
        // 씬 스코프로 등록
        ServiceLocator.Register(this, ManagerScope.Scene);
        _cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        // 비동기 정리 작업 시도
        _currentLogicState?.Exit(_cts.Token).Forget();
        _cts.Dispose();

        ServiceLocator.Unregister<BattleManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            // 필수 매니저 검증 (Fail-Fast)
            if (!ServiceLocator.TryGet(out MapManager mapManager))
                throw new Exception("MapManager가 등록되지 않았습니다!");

            if (!ServiceLocator.TryGet(out TurnManager turnManager))
                throw new Exception("TurnManager가 등록되지 않았습니다!");

            // 임시 UIManager Mock 생성
            _uiManagerMock = new UIManagerMock();
            // 실제 UIManager가 생기면: ServiceLocator.Get<IUIManager>(); 로 대체

            // [변경] BattleContext 생성 및 팩토리 초기화
            var battleContext = new BattleContext(mapManager, turnManager, _uiManagerMock);
            _stateFactory = new BattleStateFactory(battleContext);

            Debug.Log("[BattleManager] 초기화 완료. 전투 준비 상태(Setup)로 진입합니다.");

            // (임시) 테스트를 위해 UI 자동 클릭 트리거
            _uiManagerMock.AutoTriggerStartConfirmation();

            // 즉시 Setup 상태로 전환 (BootManager 기다릴 필요 없음)
            HandleTransitionRequest(BattleState.Setup, null);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] 초기화 실패: {ex.Message}");
            CurrentStateID = BattleState.Error;
            // 팩토리가 없을 수 있으므로 직접 에러 상태 생성
            _currentLogicState = new ErrorState(null); // ErrorState는 Context가 없어도 동작하도록 설계 필요
            _currentLogicState.Enter(new ErrorPayload(ex), _cts.Token).Forget();
        }
        await UniTask.CompletedTask;
    }

    private void HandleTransitionRequest(BattleState nextStateID, StatePayload payload)
    {
        TransitionAsync(nextStateID, payload).Forget();
    }

    private async UniTaskVoid TransitionAsync(BattleState nextStateID, StatePayload payload)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[BattleManager] 전환 중 중복 요청 무시됨: {nextStateID}");
            return;
        }
        _isTransitioning = true;

        try
        {
            if (_currentLogicState != null)
            {
                _currentLogicState.OnRequestTransition -= HandleTransitionRequest;
                await _currentLogicState.Exit(_cts.Token);
            }

            var oldID = CurrentStateID;

            // [변경] BattleStateFactory 사용
            _currentLogicState = _stateFactory.GetOrCreate(nextStateID);
            CurrentStateID = _currentLogicState.StateID;

            Debug.Log($"[BattleManager] State Change: {oldID} -> {CurrentStateID}");
            OnStateChanged?.Invoke(oldID, CurrentStateID);

            _currentLogicState.OnRequestTransition += HandleTransitionRequest;
            await _currentLogicState.Enter(payload, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[BattleManager] 상태 전환 중 취소됨 (게임 종료 등)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] 상태 전환 중 치명적 오류 발생. ErrorState로 전환합니다.");
            CurrentStateID = BattleState.Error;
            _currentLogicState = _stateFactory.GetOrCreate(BattleState.Error);
            _currentLogicState.Enter(new ErrorPayload(ex), _cts.Token).Forget();
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
            Debug.LogError($"[BattleManager] '{CurrentStateID}' 상태 Update 중 오류 발생.");
            HandleTransitionRequest(BattleState.Error, new ErrorPayload(ex));
        }
    }
}