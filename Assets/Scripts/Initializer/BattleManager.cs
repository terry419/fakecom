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

    // 상태 전환 플래그 (Race Condition 방지)
    private bool _isTransitioning = false;
    public bool IsTransitioning => _isTransitioning;

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
        // [Fix 2] 정적 이벤트 해제 (이미 올바름)
        BootManager.OnBootComplete -= OnSystemBootFinished;

        // [Fix 8] Exit 호출 시 안전한 정리 (Cancel 후 Forget)
        if (_currentLogicState != null)
        {
            _currentLogicState.OnRequestTransition -= HandleTransitionRequest;

            // Exit이 토큰 취소를 감지하고 빨리 끝나도록 유도
            if (_stateCts != null) _stateCts.Cancel();

            // 동기 컨텍스트이므로 Forget 사용하되, 위에서 Cancel 했으므로 안전성 확보
            _currentLogicState.Exit(CancellationToken.None).Forget();
            _currentLogicState = null;
        }

        // 토큰 정리 (순서: Cancel -> Dispose)
        if (_stateCts != null)
        {
            _stateCts.Cancel();
            _stateCts.Dispose();
        }

        if (_destroyCts != null)
        {
            _destroyCts.Cancel();
            _destroyCts.Dispose();
        }

        ServiceLocator.Unregister<BattleManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            Debug.Log("[BattleManager] Initialize Start...");

            if (!ServiceLocator.TryGet<MapManager>(out var mapManager))
                throw new InvalidOperationException("[BattleManager] MapManager not found");

            if (!ServiceLocator.TryGet<TurnManager>(out var turnManager))
                throw new InvalidOperationException("[BattleManager] TurnManager not found");

            IUIManager uiManager = null;
            if (ServiceLocator.TryGet<IUIManager>(out var realUI))
                uiManager = realUI;
            else
                uiManager = _uiManagerMock = new UIManagerMock();

            // Context 생성 및 유효성 검사
            var battleContext = new BattleContext
            {
                BattleManager = this,
                Map = mapManager,
                Turn = turnManager,
                UI = uiManager
            };

            // [Fix 3] Validate 호출
            battleContext.Validate();

            _stateFactory = new BattleStateFactory(battleContext);

            // [Fix 1] 정적 이벤트 구독 및 즉시 상태 확인
            BootManager.OnBootComplete += OnSystemBootFinished;

            if (BootManager.IsBootComplete)
            {
                Debug.Log("[BattleManager] BootManager already complete. Starting immediately.");
                OnSystemBootFinished(true);
            }
            else
            {
                Debug.Log("[BattleManager] Waiting for BootManager...");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] Initialize failed: {ex.Message}");
            await HandleCriticalError(ex);
        }
    }

    private void OnSystemBootFinished(bool isSuccess)
    {
        if (CurrentStateID == BattleState.Error) return;

        if (!isSuccess)
        {
            HandleTransitionRequest(BattleState.Error,
                new ErrorPayload(new InvalidOperationException("[BattleManager] System boot failed")));
            return;
        }

        Debug.Log("[BattleManager] System boot complete. Requesting Setup state.");
        HandleTransitionRequest(BattleState.Setup, null);
    }

    public void HandleTransitionRequest(BattleState nextState, StatePayload payload)
    {
        if (_isTransitioning)
        {
            Debug.LogWarning($"[BattleManager] 전환 중 무시됨: {CurrentStateID} -> {nextState}");
            return;
        }

        ChangeState(nextState, payload).Forget();
    }

    private async UniTask ChangeState(BattleState nextStateID, StatePayload payload = null)
    {
        _isTransitioning = true;
        var oldID = CurrentStateID;

        try
        {
            // 1. 이전 상태 종료
            if (_currentLogicState != null)
            {
                _currentLogicState.OnRequestTransition -= HandleTransitionRequest;
                await _currentLogicState.Exit(CancellationToken.None);
                _currentLogicState = null;
            }

            // 2. 토큰 갱신
            if (_stateCts != null)
            {
                _stateCts.Cancel();
                _stateCts.Dispose();
            }
            _stateCts = new CancellationTokenSource();

            // 3. 다음 상태 진입
            // [Fix 6] using 블록 내부에서는 Enter만 수행 (LinkedToken 유효성 보장)
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_destroyCts.Token, _stateCts.Token))
            {
                _currentLogicState = _stateFactory.GetOrCreate(nextStateID);
                CurrentStateID = _currentLogicState.StateID;

                _currentLogicState.OnRequestTransition += HandleTransitionRequest;

                await _currentLogicState.Enter(payload, linkedCts.Token);
            }

            // [Fix 5] Enter 완료 후, using 블록 밖에서 이벤트 호출 (Listener 예외 격리)
            try
            {
                OnStateChanged?.Invoke(oldID, CurrentStateID);
            }
            catch (Exception listenerEx)
            {
                Debug.LogError($"[BattleManager] OnStateChanged listener error: {listenerEx.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log($"[BattleManager] State change cancelled: {nextStateID}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[BattleManager] State change error: {ex.Message}");
            await HandleCriticalError(ex);
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
            Debug.LogError($"[BattleManager] Update Critical Error: {ex.Message}");
            // [Fix 7] 에러 상태 전환 요청 (finally에서 _isTransitioning 해제되므로 다음 프레임 처리가능)
            HandleTransitionRequest(BattleState.Error, new ErrorPayload(ex));
        }
    }

    private async UniTask HandleCriticalError(Exception ex)
    {
        try
        {
            _currentLogicState = null;
            CurrentStateID = BattleState.Error;

            if (_stateCts != null)
            {
                _stateCts.Cancel();
                _stateCts.Dispose();
            }
            _stateCts = new CancellationTokenSource();

            // [Fix 4] StateFactory Null 체크
            if (_stateFactory == null)
            {
                Debug.LogError("[BattleManager] StateFactory is null. Cannot enter ErrorState.");
                return;
            }

            _currentLogicState = _stateFactory.GetOrCreate(BattleState.Error);
            _currentLogicState.OnRequestTransition += HandleTransitionRequest;

            await _currentLogicState.Enter(new ErrorPayload(ex), _stateCts.Token);
        }
        catch (Exception innerEx)
        {
            Debug.LogError($"[BattleManager] Error state entry failed: {innerEx.Message}");
        }
        finally
        {
            _isTransitioning = false;
        }
    }
}