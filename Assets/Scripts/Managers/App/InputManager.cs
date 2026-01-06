using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;

public class InputManager : MonoBehaviour, IInitializable
{
    public static InputManager Instance { get; private set; }

    // ========================================================================
    // 1. 이벤트 정의
    // ========================================================================
    public event Action<Vector2> OnCommandInput; // 우클릭(명령)
    public event Action<Vector2> OnMouseMove;    // 마우스 이동
    public event Action OnTurnEndInvoked;        // 턴 종료 (9번 키)
    public event Action OnCameraRecenter;        // 카메라 복귀 (8번 키)
    public event Action<int> OnAbilityHotkeyPressed; // 숫자 키 1~7

    // [Camera Polling Data]
    public Vector2 CameraMoveVector { get; private set; }
    public float CameraKeyRotateAxis { get; private set; }

    // ========================================================================
    // 2. 내부 변수
    // ========================================================================
    private PlayerInputActions _inputActions;
    private bool _isInputActive = false;
    private bool _isPointerOverUI = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        ServiceLocator.Register(this, ManagerScope.Global);
        _inputActions = new PlayerInputActions();
    }

    private void OnDestroy()
    {
        // 이벤트 초기화
        OnCommandInput = null;
        OnMouseMove = null;
        OnTurnEndInvoked = null;
        OnCameraRecenter = null;
        OnAbilityHotkeyPressed = null;

        DisposeActions();

        if (ServiceLocator.IsRegistered<InputManager>())
            ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    public UniTask Initialize(InitializationContext context)
    {
        // [수정] .Get() 제거 및 단순 Null 체크로 변경 (안전성 확보)
        if (_inputActions == null || _inputActions.asset == null)
        {
            Debug.LogError("[InputManager] PlayerInputActions asset is missing!");
            return UniTask.CompletedTask;
        }

        SetupInputBindings();
        ResumeInput();
        Debug.Log("[InputManager] Initialized.");
        return UniTask.CompletedTask;
    }

    // ========================================================================
    // 3. 액션 바인딩
    // ========================================================================
    private void SetupInputBindings()
    {
        _inputActions.Player.Select.performed += ctx =>
        {
            if (_isInputActive && !_isPointerOverUI)
            {
                Vector2 mousePos = _inputActions.Player.Point.ReadValue<Vector2>();
                OnCommandInput?.Invoke(mousePos);
            }
        };

        // 1) Command (우클릭) - [수정] RightClick -> Command
        _inputActions.Player.RightClick.performed += ctx =>
        {
            if (_isInputActive && !_isPointerOverUI)
            {
                Vector2 mousePos = _inputActions.Player.Point.ReadValue<Vector2>();
                OnCommandInput?.Invoke(mousePos);
            }
        };

        // 2) TurnEnd (9번 키)
        _inputActions.Player.TurnEnd.performed += _ => OnTurnEndInvoked?.Invoke();

        // 3) Recenter (8번 키)
        _inputActions.Player.Recenter.performed += _ => OnCameraRecenter?.Invoke();
    }

    private void DisposeActions()
    {
        if (_inputActions != null)
        {
            _inputActions.Player.Disable();
            _inputActions.Dispose();
        }
    }

    // ========================================================================
    // 4. Update Loop
    // ========================================================================
    private void Update()
    {
        if (!_isInputActive) return;

        _isPointerOverUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        OnMouseMove?.Invoke(_inputActions.Player.Point.ReadValue<Vector2>());

        CameraMoveVector = _inputActions.Player.CameraMove.ReadValue<Vector2>();
        CameraKeyRotateAxis = _inputActions.Player.CameraRotate.ReadValue<float>();

        HandleNumberKeys();
    }

    private void HandleNumberKeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // [수정] 1~7번 키 안전한 루프 처리
        for (int i = 1; i <= 7; i++)
        {
            // Key.Digit1(50) + 0 = Key.Digit1
            Key key = Key.Digit1 + (i - 1);

            if (keyboard[key].wasPressedThisFrame)
            {
                OnAbilityHotkeyPressed?.Invoke(i);
                return;
            }
        }
    }

    // ========================================================================
    // 5. 외부 제어 및 헬퍼
    // ========================================================================
    public void InvokeTurnEnd() => OnTurnEndInvoked?.Invoke();
    public void PauseInput() => SetInputActive(false);
    public void ResumeInput() => SetInputActive(true);

    private void SetInputActive(bool active)
    {
        _isInputActive = active;
        if (_inputActions == null) return;

        if (active)
            _inputActions.Player.Enable();
        else
        {
            _inputActions.Player.Disable();
            CameraMoveVector = Vector2.zero;
            CameraKeyRotateAxis = 0f;
        }
    }

    public bool IsCameraRotatePressed()
    {
        // [수정] RightClick -> Command
        return _inputActions != null && _inputActions.Player.RightClick.IsPressed();
    }

    public Vector2 GetMouseDelta()
    {
        return _inputActions != null ? _inputActions.Player.MouseDelta.ReadValue<Vector2>() : Vector2.zero;
    }
}