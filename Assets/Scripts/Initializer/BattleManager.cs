using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

// FSM의 총괄 관리자
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
        ServiceLocator.Register(this, ManagerScope.Scene);
        _cts = new CancellationTokenSource();
    }

    private void OnDestroy()
    {
        _cts.Cancel();
        // OnDestroy는 동기 컨텍스트이므로, Exit의 비동기 완료를 기다릴 수 없습니다.
        // Forget()으로 호출하여 마지막 정리 작업을 시도하게 합니다.
        _currentLogicState?.Exit(_cts.Token).Forget();
        _cts.Dispose();

        ServiceLocator.Unregister<BattleManager>(ManagerScope.Scene);
        BootManager.OnBootComplete -= OnSystemBootFinished;
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

            // 임시 UIManager Mock 생성 및 Context에 전달
            _uiManagerMock = new UIManagerMock();
            // 실제 UIManager 사용 시: ServiceLocator.Register<IUIManager>(_uiManagerMock, ManagerScope.Scene);

            // 의존성 컨테이너(SessionContext) 생성 및 팩토리 초기화
            var BattleContext = new BattleContext(mapManager, turnManager, _uiManagerMock);
            _stateFactory = new BattleStateFactory(BattleContext);

            BootManager.OnBootComplete += OnSystemBootFinished;
        }
        catch (Exception ex)
        {
            // 초기화 실패 시 즉시 ErrorState로 전환
            Debug.LogError($"[BattleManager] 초기화 실패: {ex.Message}");
            CurrentStateID = BattleState.Error;
            // _stateFactory가 없을 수 있으므로 ErrorState를 직접 생성
            _currentLogicState = new ErrorState(null);
            _currentLogicState.Enter(new ErrorPayload(ex), _cts.Token).Forget();
        }
        await UniTask.CompletedTask;
    }

    private void OnSystemBootFinished(bool isSuccess)
    {
        // 초기화 단계에서 이미 ErrorState로 전환되었다면 무시
        if (CurrentStateID == BattleState.Error) return;

        if (!isSuccess || _stateFactory == null)
        {
            HandleTransitionRequest(BattleState.Error, new ErrorPayload(new Exception("Boot failed or StateFactory not ready.")));
            return;
        }

        Debug.Log("[BattleManager] 시스템 부팅 완료. Setup 상태 진입 요청.");

        // (임시) Mock UI가 버튼을 자동으로 누르도록 하여 테스트 진행
        _uiManagerMock.AutoTriggerStartConfirmation();

        HandleTransitionRequest(BattleState.Setup, null);
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
            _currentLogicState = _stateFactory.GetOrCreate(nextStateID);
            CurrentStateID = _currentLogicState.StateID;

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
            // 현재 전환 로직이 실패했으므로, 다시 이 메서드를 호출하지 않고 직접 ErrorState로 진입
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
            Debug.LogError($"[BattleManager] '{CurrentStateID}' 상태 Update 중 오류 발생. ErrorState로 전환합니다.");
            HandleTransitionRequest(BattleState.Error, new ErrorPayload(ex));
        }
    }
}
