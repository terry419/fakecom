using UnityEngine;
using System;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using Cysharp.Threading.Tasks;

public class InputManager : MonoBehaviour, IInitializable, GameInput.IPlayerActions
{
    // ========================================================================
    // 1. 이벤트 정의 (Naming Collision 해결)
    // ========================================================================
    public event Action<Vector2> OnSelectInput;   // 좌클릭
    public event Action<Vector2> OnCommandInput;  // 우클릭
    public event Action OnCancelInput;            // 취소

    // 내부 변수
    private GameInput _gameInput;
    private bool _isPointerOverUI = false; // [Fix 5] UI 상태 캐싱

    // ========================================================================
    // 2. 초기화 및 생명주기 (Dispose 추가)
    // ========================================================================
    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
        _gameInput = new GameInput();
        _gameInput.Player.SetCallbacks(this);
    }

    private void OnEnable() => _gameInput.Player.Enable();
    private void OnDisable() => _gameInput.Player.Disable();

    private void OnDestroy()
    {
        // [Fix 4] 메모리 누수 방지
        _gameInput?.Dispose();
        ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    public UniTask Initialize(InitializationContext context) => UniTask.CompletedTask;

    // ========================================================================
    // 3. UI 상태 캐싱 (Performance Optimization)
    // ========================================================================
    private void Update()
    {
        // [Fix 5] 매 입력마다 EventSystem을 호출하지 않고, 프레임당 1회만 체크
        if (EventSystem.current != null)
        {
            _isPointerOverUI = EventSystem.current.IsPointerOverGameObject();
        }
    }

    // ========================================================================
    // 4. GameInput 인터페이스 구현 (Callback)
    // ========================================================================

    // [Fix 1] 메서드 이름(OnSelect)과 이벤트 이름(OnSelectInput) 분리
    public void OnSelect(InputAction.CallbackContext context)
    {
        if (context.performed && !_isPointerOverUI)
        {
            OnSelectInput?.Invoke(GetPointerPosition());
        }
    }

    public void OnCommand(InputAction.CallbackContext context)
    {
        if (context.performed && !_isPointerOverUI)
        {
            OnCommandInput?.Invoke(GetPointerPosition());
        }
    }

    public void OnCancel(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnCancelInput?.Invoke();
        }
    }

    // ========================================================================
    // 5. 안전한 포인터 위치 가져오기
    // ========================================================================
    private Vector2 GetPointerPosition()
    {
        // [Fix 2] Mouse.current가 null일 경우(패드 연결 등) 대비
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }

        // 터치나 펜 등 다른 포인터 장치 시도
        if (Pointer.current != null)
        {
            return Pointer.current.position.ReadValue();
        }

        // 아무것도 없으면 화면 중앙 반환 (예외 방지)
        return new Vector2(Screen.width / 2f, Screen.height / 2f);
    }
}