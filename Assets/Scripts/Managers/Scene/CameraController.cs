using UnityEngine;
using Cysharp.Threading.Tasks;

// [RequireComponent]를 사용하여 하위 컴포넌트가 자동으로 추가되게 합니다.
[RequireComponent(typeof(CameraShaker))]
[RequireComponent(typeof(CameraActionView))]
public class CameraController : MonoBehaviour, IInitializable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float mouseRotationSpeed = 0.2f;
    [SerializeField] private float smoothing = 5f;

    [Header("Rotation Defaults")]
    [SerializeField] private float defaultPitch = 45f;
    [SerializeField] private float defaultYaw = 45f;

    [Header("Zoom Settings")]
    [SerializeField] private float fixedZoomDistance = 14f;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    [Header("Debug Settings")]
    [SerializeField] private Transform debugAttacker;
    [SerializeField] private Transform debugTarget;

    // --- 내부 컴포넌트 참조 ---
    private Transform boomTransform;
    private Transform shakeHolderTransform;
    private Transform cameraTransform;

    // --- 분리된 로직 컴포넌트 ---
    private CameraShaker _shaker;
    private CameraActionView _actionView;

    // --- 외부 매니저 ---
    private InputManager inputManager;
    private TurnManager _turnManager; // [TurnManager 의존성 역전]

    // --- 상태 변수 ---
    private Vector3 targetPosition;
    [SerializeField] private float currentYaw;
    [SerializeField] private float currentPitch;

    private void Awake()
    {
        InitializeTransforms();

        // 컴포넌트 가져오기
        _shaker = GetComponent<CameraShaker>();
        _actionView = GetComponent<CameraActionView>();
    }

    private void InitializeTransforms()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CameraController] 자식에 Camera가 없습니다!");
            enabled = false;
            return;
        }
        cameraTransform = cam.transform;

        boomTransform = transform.Find("CameraBoom");
        if (boomTransform == null)
        {
            GameObject boomObj = new GameObject("CameraBoom");
            boomTransform = boomObj.transform;
            boomTransform.SetParent(transform, false);
        }

        shakeHolderTransform = boomTransform.Find("ShakeHolder");
        if (shakeHolderTransform == null)
        {
            GameObject holderObj = new GameObject("ShakeHolder");
            shakeHolderTransform = holderObj.transform;
            shakeHolderTransform.SetParent(boomTransform, false);
        }

        if (cameraTransform.parent != shakeHolderTransform)
        {
            cameraTransform.SetParent(shakeHolderTransform);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }

        targetPosition = transform.position;
    }

    private void OnDestroy()
    {
        // 이벤트 해제
        if (_turnManager != null) _turnManager.OnTurnStarted -= HandleTurnStarted;
        if (inputManager != null) inputManager.OnCameraRecenter -= HandleRecenterRequest;

        if (ServiceLocator.IsRegistered<CameraController>())
            ServiceLocator.Unregister<CameraController>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);

        inputManager = ServiceLocator.Get<InputManager>();
        _turnManager = ServiceLocator.Get<TurnManager>();

        // 하위 컴포넌트 초기화
        _shaker.Initialize(cameraTransform);
        _actionView.Initialize(transform, boomTransform, shakeHolderTransform);

        // 초기값 설정
        currentPitch = defaultPitch;
        currentYaw = defaultYaw;

        if (boomTransform != null)
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        if (shakeHolderTransform != null)
            shakeHolderTransform.localPosition = new Vector3(0, 0, -fixedZoomDistance);

        // 이벤트 구독 (옵저버 패턴)
        if (_turnManager != null) _turnManager.OnTurnStarted += HandleTurnStarted;
        if (inputManager != null) inputManager.OnCameraRecenter += HandleRecenterRequest;

        Debug.Log("[CameraController] Initialized & Components Linked.");
        await UniTask.CompletedTask;
    }

    private void LateUpdate()
    {
        // 1. 액션 뷰 상태라면 위임
        if (_actionView.IsActive)
        {
            _actionView.UpdateView();
        }
        // 2. 일반 뷰 상태라면 직접 처리
        else
        {
            HandleNormalView();
        }
    }

    private void HandleNormalView()
    {
        if (inputManager == null) return;

        // 이동 입력
        Vector2 moveInput = inputManager.CameraMoveVector;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion heading = Quaternion.Euler(0, currentYaw, 0);
            Vector3 forward = heading * Vector3.forward;
            Vector3 right = heading * Vector3.right;
            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

            targetPosition += moveDir * moveSpeed * Time.deltaTime;
        }

        // 회전 입력
        if (inputManager.IsCameraRotatePressed())
            currentYaw += inputManager.GetMouseDelta().x * mouseRotationSpeed;

        float keyRotation = inputManager.CameraKeyRotateAxis;
        if (Mathf.Abs(keyRotation) > 0.01f)
            currentYaw += keyRotation * rotationSpeed * Time.deltaTime;

        ApplyNormalTransform();
    }

    private void ApplyNormalTransform()
    {
        // 위치 보간
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);

        // 회전 보간
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        boomTransform.rotation = Quaternion.Slerp(boomTransform.rotation, targetRotation, Time.deltaTime * smoothing);

        // 줌 거리 복구 (액션 뷰에서 돌아왔을 때를 대비)
        if (shakeHolderTransform != null)
        {
            Vector3 targetLocalPos = new Vector3(0, 0, -fixedZoomDistance);
            shakeHolderTransform.localPosition = Vector3.Lerp(shakeHolderTransform.localPosition, targetLocalPos, Time.deltaTime * smoothing);
        }
    }

    // --- 외부 제어 메서드 ---

    public void SetTarget(Transform target, bool immediate = false)
    {
        if (target == null) return;

        // 액션 뷰 해제
        if (_actionView.IsActive) _actionView.Disable();

        targetPosition = target.position;
        _shaker.Stop(); // 타겟 변경 시 쉐이크 중단

        if (immediate)
        {
            transform.position = targetPosition;
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
            if (shakeHolderTransform != null)
                shakeHolderTransform.localPosition = new Vector3(0, 0, -fixedZoomDistance);
        }
    }

    public void PlayImpactShake()
    {
        // 설정값을 사용하여 쉐이커 호출
        _shaker.Play(shakeDuration, shakeMagnitude);
    }

    public void EnableActionView(Transform attacker, Transform target)
    {
        _shaker.Stop();
        _actionView.Enable(attacker, target);
    }

    public void DisableActionView()
    {
        _actionView.Disable();
        targetPosition = transform.position; // 현재 위치에서 다시 시작
        _shaker.Stop();
    }

    public void ToggleDebugActionView()
    {
        if (_actionView.IsActive) DisableActionView();
        else if (debugAttacker != null && debugTarget != null) EnableActionView(debugAttacker, debugTarget);
    }

    // --- 이벤트 핸들러 (TurnManager 의존성 제거됨) ---

    private void HandleTurnStarted(UnitStatus unit)
    {
        if (unit != null) SetTarget(unit.transform, false);
    }

    private void HandleRecenterRequest()
    {
        if (_turnManager != null && _turnManager.ActiveUnit != null)
        {
            SetTarget(_turnManager.ActiveUnit.transform, true);
        }
    }
}