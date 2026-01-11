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
        // ServiceLocator 등록 (Scene Scope or Global Scope)
        // 여기서는 Global로 등록되어 있으나, 씬 전환 시 파괴 정책에 따라 Scene으로 변경 고려 가능
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
        // [Check] PlayerInputActions 에셋에 'RightClick' 액션이 정의되어 있는지 확인하십시오.
        // 만약 없다면 'Command' 등 우클릭에 해당하는 액션으로 변경해야 합니다.
        // 예: return _inputActions != null && _inputActions.Player.Command.IsPressed();
        return _inputActions != null && _inputActions.Player.RightClick.IsPressed();
    }

    public Vector2 GetMouseDelta()
    {
        return _inputActions != null ? _inputActions.Player.MouseDelta.ReadValue<Vector2>() : Vector2.zero;
    }
}