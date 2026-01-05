using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class InputManager : MonoBehaviour, IInitializable
{
    // ========================================================================
    // 1. 이벤트 정의 (외부 시스템 연결용)
    // ========================================================================
    // [Unit/Interaction]
    public event Action<Vector2> OnCommandInput; // 이동/공격 지점 클릭
    public event Action<Vector2> OnMouseMove;    // 마우스 포인터 이동

    // [Turn System] -> TurnManager가 구독
    public event Action OnTurnEndInvoked;        // 턴 종료 (Space/Enter)

    // [Camera System] -> CameraController가 구독
    public event Action OnCameraRecenter;        // 카메라 복귀 (R key)
    public event Action<float> OnCameraZoom;     // 휠 줌

    // [Camera Polling] -> CameraController가 Update에서 참조
    public Vector2 CameraMoveVector { get; private set; }
    public float CameraKeyRotateAxis { get; private set; }

    // ========================================================================
    // 2. Input Actions (내부 정의)
    // ========================================================================
    private InputAction _clickAction;
    private InputAction _pointAction;
    private InputAction _turnEndAction;
    private InputAction _cameraRecenterAction;
    private InputAction _cameraMoveAction;
    private InputAction _cameraRotateAction;
    private InputAction _cameraZoomAction;
    private InputAction _rightClickAction;
    private InputAction _mouseDeltaAction;

    private bool _isInputActive = false;

    private void Awake()
    {
        if (ServiceLocator.IsRegistered<InputManager>())
        {
            Destroy(gameObject);
            return;
        }
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        DisposeActions();
        if (ServiceLocator.IsRegistered<InputManager>())
            ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    public Cysharp.Threading.Tasks.UniTask Initialize(InitializationContext context)
    {
        SetupInputActions();
        ResumeInput(); // 초기화 완료 후 입력 활성화
        Debug.Log("[InputManager] Initialized & Actions Bound.");
        return Cysharp.Threading.Tasks.UniTask.CompletedTask;
    }

    // ========================================================================
    // 3. 액션 정의 및 바인딩 (Code-based Setup)
    // ========================================================================
    private void SetupInputActions()
    {
        // 1) 클릭 (좌클릭)
        _clickAction = new InputAction("Click", type: InputActionType.Button);
        _clickAction.AddBinding("<Mouse>/leftButton");
        _clickAction.performed += OnClickPerformed;

        _rightClickAction = new InputAction("RightClick", type: InputActionType.Button);
        _rightClickAction.AddBinding("<Mouse>/rightButton");

        // [추가] 마우스 이동량 (Delta)
        _mouseDeltaAction = new InputAction("MouseDelta", type: InputActionType.Value);
        _mouseDeltaAction.AddBinding("<Mouse>/delta");

        // 2) 마우스 위치
        _pointAction = new InputAction("Point", type: InputActionType.Value);
        _pointAction.AddBinding("<Mouse>/position");

        // 3) 턴 종료 (Space)
        _turnEndAction = new InputAction("TurnEnd", type: InputActionType.Button);
        _turnEndAction.AddBinding("<Keyboard>/space");
        _turnEndAction.AddBinding("<Keyboard>/enter");
        _turnEndAction.performed += _ => OnTurnEndInvoked?.Invoke();

        // 4) 카메라 리센터 (R)
        _cameraRecenterAction = new InputAction("Recenter", type: InputActionType.Button);
        _cameraRecenterAction.AddBinding("<Keyboard>/r");
        _cameraRecenterAction.performed += _ => OnCameraRecenter?.Invoke();

        // 5) 카메라 이동 (WASD / 화살표) - 2D Vector Composite
        _cameraMoveAction = new InputAction("CameraMove", type: InputActionType.Value);
        _cameraMoveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow");

        // 6) 카메라 회전 (Q/E) - 1D Axis
        _cameraRotateAction = new InputAction("CameraRotate", type: InputActionType.Value);
        _cameraRotateAction.AddCompositeBinding("1DAxis")
            .With("Negative", "<Keyboard>/q")
            .With("Positive", "<Keyboard>/e");

        // 7) 줌 (Mouse Scroll)
        _cameraZoomAction = new InputAction("CameraZoom", type: InputActionType.Value);
        _cameraZoomAction.AddBinding("<Mouse>/scroll/y");
        _cameraZoomAction.performed += ctx => OnCameraZoom?.Invoke(ctx.ReadValue<float>());
    }

    private void DisposeActions()
    {
        _clickAction?.Dispose();
        _pointAction?.Dispose();
        _turnEndAction?.Dispose();
        _cameraRecenterAction?.Dispose();
        _cameraMoveAction?.Dispose();
        _cameraRotateAction?.Dispose();
        _cameraZoomAction?.Dispose();
        _rightClickAction?.Dispose();
        _mouseDeltaAction?.Dispose();
    }

    // ========================================================================
    // 4. Update Loop (Polling 처리)
    // ========================================================================
    private void Update()
    {
        if (!_isInputActive) return;

        // 마우스 이동 이벤트 전파
        OnMouseMove?.Invoke(_pointAction.ReadValue<Vector2>());

        // 카메라 이동/회전 값 갱신 (Polling)
        CameraMoveVector = _cameraMoveAction.ReadValue<Vector2>();
        CameraKeyRotateAxis = _cameraRotateAction.ReadValue<float>();
    }

    // ========================================================================
    // 5. 핸들러 및 상태 관리
    // ========================================================================
    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isInputActive) return;

        // UI 클릭 방지
        if (EventSystem.current?.IsPointerOverGameObject() == true) return;

        Vector2 mousePos = _pointAction.ReadValue<Vector2>();
        OnCommandInput?.Invoke(mousePos);
    }

    // UI 버튼에서 호출 가능한 턴 종료 메서드
    public void InvokeTurnEnd()
    {
        OnTurnEndInvoked?.Invoke();
    }

    public void PauseInput() => SetInputActive(false);
    public void ResumeInput() => SetInputActive(true);

    private void SetInputActive(bool active)
    {
        if (_isInputActive == active) return;
        _isInputActive = active;

        if (active)
        {
            _clickAction?.Enable();
            _pointAction?.Enable();
            _rightClickAction?.Enable();
            _mouseDeltaAction?.Enable();
            _turnEndAction?.Enable();
            _cameraRecenterAction?.Enable();
            _cameraMoveAction?.Enable();
            _cameraRotateAction?.Enable();
            _cameraZoomAction?.Enable();
        }
        else
        {
            _clickAction?.Disable();
            _pointAction?.Disable();
            _rightClickAction?.Enable();
            _mouseDeltaAction?.Enable();
            _turnEndAction?.Disable();
            _cameraRecenterAction?.Disable();
            _cameraMoveAction?.Disable();
            _cameraRotateAction?.Disable();
            _cameraZoomAction?.Disable();
        }
    }

    public bool IsCameraRotatePressed()
    {
        return _rightClickAction != null && _rightClickAction.IsPressed();
    }

    public Vector2 GetMouseDelta()
    {
        return _mouseDeltaAction != null ? _mouseDeltaAction.ReadValue<Vector2>() : Vector2.zero;
    }
}