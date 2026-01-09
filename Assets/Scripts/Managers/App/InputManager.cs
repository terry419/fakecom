using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;
using System;

public class InputManager : MonoBehaviour, IInitializable
{
    // [수정 1] static Instance 및 중복 파괴 로직 제거 (ServiceLocator가 유일성 보장)
    // public static InputManager Instance { get; private set; } 

    // ========================================================================
    // 1. 이벤트 정의
    // ========================================================================
    public event Action<Vector2> OnCommandInput;
    public event Action<Vector2> OnMouseMove;
    public event Action OnTurnEndInvoked;
    public event Action OnCameraRecenter;
    public event Action<int> OnAbilityHotkeyPressed;

    public Vector2 CameraMoveVector { get; private set; }
    public float CameraKeyRotateAxis { get; private set; }

    private PlayerInputActions _inputActions;
    private bool _isInputActive = false;
    private bool _isPointerOverUI = false;

    private void Awake()
    {
        // [수정 2] 싱글톤 로직 삭제 -> 바로 등록
        ServiceLocator.Register(this, ManagerScope.Global);
        _inputActions = new PlayerInputActions();
    }

    private void OnDestroy()
    {
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

    // ... (이하 나머지 코드는 기존과 동일하게 유지) ...
    // SetupInputBindings, DisposeActions, Update, HandleNumberKeys 등등

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

        _inputActions.Player.TurnEnd.performed += _ => OnTurnEndInvoked?.Invoke();
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
        return _inputActions != null && _inputActions.Player.RightClick.IsPressed(); // 이름 수정
    }

    public Vector2 GetMouseDelta()
    {
        return _inputActions != null ? _inputActions.Player.MouseDelta.ReadValue<Vector2>() : Vector2.zero;
    }
}