using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;

public class InputManager : MonoBehaviour, IInitializable
{
    // ========================================================================
    // 1. 이벤트 정의
    // ========================================================================
    public event Action<Vector2> OnCommandInput;      // Select (우클릭)
    public event Action<Vector2> OnMouseMove;         // Point (마우스 위치 - 변화 시)
    public event Action OnTurnEndInvoked;             // TurnEnd
    public event Action OnCameraRecenter;             // Recenter
    public event Action<int> OnAbilityHotkeyPressed;  // 숫자키 1~7

    // [QTE 전용 이벤트]
    public event Action<bool> OnQTEInput;             // true: Hold, false: Release

    // ========================================================================
    // 2. 프로퍼티 및 상태
    // ========================================================================
    public Vector2 CameraMoveVector { get; private set; }
    public float CameraKeyRotateAxis { get; private set; }

    private PlayerInputActions _inputActions;

    private bool _isInputActive = false;
    public bool IsQTEContext { get; private set; } = false;
    private bool _isPointerOverUI = false;

    // 마우스 비교용 캐시
    private Vector2 _lastMousePos;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
        _inputActions = new PlayerInputActions();
    }

    private void OnDestroy()
    {
        // [안전장치] 외부 연결(InputSystem) 먼저 해제
        DisposeActions();

        if (ServiceLocator.IsRegistered<InputManager>())
            ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    // ========================================================================
    // 3. 초기화 (IInitializable)
    // ========================================================================
    public async UniTask Initialize(InitializationContext context)
    {
        if (_inputActions == null)
        {
            Debug.LogError("[InputManager] PlayerInputActions asset is missing!");
            return;
        }

        SetupInputBindings();

        // 초기화 시 입력 활성화
        SetInputActive(true);

        Debug.Log("[InputManager] Initialized.");
        await UniTask.CompletedTask;
    }

    private void SetupInputBindings()
    {
        // [Player 맵]
        _inputActions.Player.Select.performed += OnSelectPerformed;
        _inputActions.Player.TurnEnd.performed += OnTurnEndPerformed;
        _inputActions.Player.Recenter.performed += OnRecenterPerformed;

        // [QTE 맵]
        _inputActions.QTE.Interact.performed += OnQTEInteractPerformed;
        _inputActions.QTE.Interact.canceled += OnQTEInteractCanceled;
    }

    private void DisposeActions()
    {
        if (_inputActions == null) return;

        // 구독 해제
        _inputActions.Player.Select.performed -= OnSelectPerformed;
        _inputActions.Player.TurnEnd.performed -= OnTurnEndPerformed;
        _inputActions.Player.Recenter.performed -= OnRecenterPerformed;

        _inputActions.QTE.Interact.performed -= OnQTEInteractPerformed;
        _inputActions.QTE.Interact.canceled -= OnQTEInteractCanceled;

        _inputActions.Disable();
        _inputActions.Dispose();
        _inputActions = null;
    }

    // ========================================================================
    // 4. Update 루프
    // ========================================================================
    private void Update()
    {
        // 1. 전체 비활성 체크
        if (!_isInputActive) return;

        // 2. [QTE 방어] QTE 모드라면 일반 게임플레이 로직 전면 차단
        if (IsQTEContext) return;

        // 3. UI 포커스 체크
        _isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        // 4. 폴링 데이터 읽기
        // (Player Map이 Disable되어도 값을 읽을 수 있으므로 QTE 모드일 땐 위에서 리턴해야 함)
        CameraMoveVector = _inputActions.Player.CameraMove.ReadValue<Vector2>();
        CameraKeyRotateAxis = _inputActions.Player.CameraRotate.ReadValue<float>();

        // 5. 마우스 위치 최적화 (값이 변했을 때만 이벤트 발생)
        Vector2 currentMousePos = _inputActions.Player.Point.ReadValue<Vector2>();
        if (currentMousePos != _lastMousePos)
        {
            _lastMousePos = currentMousePos;
            OnMouseMove?.Invoke(currentMousePos);
        }

        // 6. 숫자키 처리
        HandleNumberKeys();
    }

    private void HandleNumberKeys()
    {
        // [수정] 중복 방어 제거 (Update 최상단에서 이미 처리됨)
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 1; i <= 7; i++)
        {
            Key key = Key.Digit1 + (i - 1);
            if (keyboard[key].wasPressedThisFrame)
            {
                OnAbilityHotkeyPressed?.Invoke(i);
                return;
            }
        }
    }

    // ========================================================================
    // 5. 컨텍스트 관리
    // ========================================================================
    public void SetInputActive(bool active)
    {
        if (_isInputActive == active) return;
        _isInputActive = active;
        UpdateInputState();
    }

    public void SetQTEContext(bool active)
    {
        if (IsQTEContext == active) return;
        IsQTEContext = active;
        UpdateInputState();
    }

    private void UpdateInputState()
    {
        if (_inputActions == null) return;

        // Player Map: 입력 ON && QTE OFF
        if (_isInputActive && !IsQTEContext)
        {
            _inputActions.Player.Enable();
        }
        else
        {
            _inputActions.Player.Disable();
            // 잔여 값 초기화
            CameraMoveVector = Vector2.zero;
            CameraKeyRotateAxis = 0f;
            // _lastMousePos는 초기화하지 않음 (커서 위치 유지)
        }

        // QTE Map: 입력 ON && QTE ON
        if (_isInputActive && IsQTEContext)
        {
            _inputActions.QTE.Enable();
        }
        else
        {
            _inputActions.QTE.Disable();
        }
    }

    // ========================================================================
    // 6. 이벤트 핸들러
    // ========================================================================
    private void OnSelectPerformed(InputAction.CallbackContext context)
    {
        if (_isPointerOverUI) return;
        // [수정] 최적화: Update에서 갱신된 캐시값 사용
        OnCommandInput?.Invoke(_lastMousePos);
    }

    private void OnTurnEndPerformed(InputAction.CallbackContext context)
    {
        // [수정] UI 체크 추가
        if (_isPointerOverUI) return;
        OnTurnEndInvoked?.Invoke();
    }

    private void OnRecenterPerformed(InputAction.CallbackContext context)
    {
        // [수정] UI 체크 추가
        if (_isPointerOverUI) return;
        OnCameraRecenter?.Invoke();
    }

    private void OnQTEInteractPerformed(InputAction.CallbackContext context) => OnQTEInput?.Invoke(true);
    private void OnQTEInteractCanceled(InputAction.CallbackContext context) => OnQTEInput?.Invoke(false);

    // ========================================================================
    // 7. 외부 접근자
    // ========================================================================
    public void InvokeTurnEnd() => OnTurnEndInvoked?.Invoke();
    public void PauseInput() => SetInputActive(false);
    public void ResumeInput() => SetInputActive(true);
    public bool IsCameraRotatePressed() => _inputActions != null && _inputActions.Player.RightClick.IsPressed();
    public Vector2 GetMouseDelta() => _inputActions != null ? _inputActions.Player.MouseDelta.ReadValue<Vector2>() : Vector2.zero;

    // ========================================================================
    // [검증 기능] 1단계 InputManager 검증 (에디터 전용)
    // ========================================================================
#if UNITY_EDITOR
    [ContextMenu("Debug/Toggle QTE Context")]
    private void DebugToggleQTE()
    {
        bool nextState = !IsQTEContext;
        Debug.Log($"<color=cyan>[InputDebug] QTE Context Toggled: {IsQTEContext} -> {nextState}</color>");
        SetQTEContext(nextState);
        // [수정] 구독 로직 제거 (안전하게 상태 전환만 수행)
    }
#endif
}