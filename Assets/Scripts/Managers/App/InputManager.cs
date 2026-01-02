using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Issue #3 해결을 위해 추가

public class InputManager : MonoBehaviour, IInitializable
{
    // [GDD 6.1] 하드웨어 입력을 게임 Action으로 변환하여 전파
    public event Action<Vector2> OnCommandInput; // 클릭/선택 (좌표 포함)
    public event Action<Vector2> OnMouseMove;    // 마우스 이동

    // New Input System 액션 정의
    private InputAction _clickAction;
    private InputAction _pointAction; // 마우스 포인터 위치

    private bool _isInputActive = false; // 초기값 false (Initialize에서 켜짐)

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
        // 액션 해제 및 메모리 정리
        _clickAction?.Dispose();
        _pointAction?.Dispose();

        if (ServiceLocator.IsRegistered<InputManager>())
            ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    public Cysharp.Threading.Tasks.UniTask Initialize(InitializationContext context)
    {
        SetupInputActions();

        // [Issue #1 해결] Setup에서는 정의만 하고, 여기서 명시적으로 활성화
        ResumeInput();

        Debug.Log("[InputManager] Initialized with New Input System.");
        return Cysharp.Threading.Tasks.UniTask.CompletedTask;
    }

    private void SetupInputActions()
    {
        // 1. 클릭 액션 (PC 좌클릭 + 게임패드 A버튼)
        _clickAction = new InputAction(name: "Click", type: InputActionType.Button);
        _clickAction.AddBinding("<Mouse>/leftButton");
        _clickAction.AddBinding("<Gamepad>/buttonSouth"); // Xbox 'A'

        // 2. 포인터 위치 액션 (마우스 좌표)
        _pointAction = new InputAction(name: "Point", type: InputActionType.Value);
        _pointAction.AddBinding("<Mouse>/position");

        // 3. 이벤트 연결
        _clickAction.performed += OnClickPerformed;

        // [Issue #1 해결] 여기서 .Enable()을 호출하지 않음. SetInputActive에서 관리.
    }

    private void Update()
    {
        if (!_isInputActive) return;

        // 마우스 이동 이벤트 매 프레임 발생
        // [Issue #4] PointAction은 Value 타입이므로 ReadValue가 효율적
        OnMouseMove?.Invoke(_pointAction.ReadValue<Vector2>());
    }

    private void OnClickPerformed(InputAction.CallbackContext ctx)
    {
        if (!_isInputActive) return;

        // [Issue #3 해결] Null 조건 연산자(?.)로 EventSystem 체크 간소화
        if (EventSystem.current?.IsPointerOverGameObject() == true)
        {
            return;
        }

        // 클릭 시점의 좌표 가져오기
        Vector2 mousePos = _pointAction.ReadValue<Vector2>();
        Debug.Log($"[InputManager] Command Input Detected at {mousePos}");

        OnCommandInput?.Invoke(mousePos);
    }

    // [Issue #2 해결] API 명확화: Pause / Resume 제공
    public void PauseInput() => SetInputActive(false);
    public void ResumeInput() => SetInputActive(true);

    private void SetInputActive(bool active)
    {
        // [Issue #1 해결] 상태가 변할 때만 Enable/Disable 수행
        if (_isInputActive == active) return;

        _isInputActive = active;
        if (active)
        {
            _clickAction?.Enable();
            _pointAction?.Enable();
        }
        else
        {
            _clickAction?.Disable();
            _pointAction?.Disable();
        }
    }
}