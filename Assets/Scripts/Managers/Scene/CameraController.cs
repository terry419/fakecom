using UnityEngine;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(CameraShaker))]
[RequireComponent(typeof(CameraActionView))]
public class CameraController : MonoBehaviour, IInitializable
{
    [Header("Component References")]
    [SerializeField] private Transform boomTransform;
    [SerializeField] private Transform shakeHolderTransform;
    private Transform cameraTransform;

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

    private CameraShaker _shaker;
    private CameraActionView _actionView;

    private InputManager inputManager;
    private TurnManager _turnManager;

    private Vector3 targetPosition;
    [SerializeField] private float currentYaw;
    [SerializeField] private float currentPitch;

    private bool _isInitialized = false;

    private void Awake()
    {
        InitializeTransforms();

        _shaker = GetComponent<CameraShaker>();
        _actionView = GetComponent<CameraActionView>();

        ApplyInitialSettings();
    }

    private void InitializeTransforms()
    {
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CameraController] 자식에 Camera 컴포넌트가 없습니다!", gameObject);
            enabled = false;
            return;
        }
        cameraTransform = cam.transform;

        if (boomTransform == null)
        {
            boomTransform = transform.Find("CameraBoom");
            if (boomTransform == null)
            {
                var go = new GameObject("CameraBoom");
                go.transform.SetParent(transform, false);
                boomTransform = go.transform;
            }
        }

        if (shakeHolderTransform == null)
        {
            shakeHolderTransform = boomTransform.Find("ShakeHolder");
            if (shakeHolderTransform == null)
            {
                var go = new GameObject("ShakeHolder");
                go.transform.SetParent(boomTransform, false);
                shakeHolderTransform = go.transform;
            }
        }

        if (cameraTransform.parent != shakeHolderTransform)
        {
            cameraTransform.SetParent(shakeHolderTransform);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }

        targetPosition = transform.position;
    }

    private Vector3 GetShakeHolderLocalPos()
    {
        return new Vector3(0, 0, -fixedZoomDistance);
    }

    private void ApplyInitialSettings()
    {
        currentPitch = defaultPitch;
        currentYaw = defaultYaw;

        if (boomTransform != null)
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        if (shakeHolderTransform != null)
            shakeHolderTransform.localPosition = GetShakeHolderLocalPos();
    }

    public async UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);

        inputManager = ServiceLocator.Get<InputManager>();
        _turnManager = ServiceLocator.Get<TurnManager>();

        if (inputManager == null || _turnManager == null)
        {
            Debug.LogError("[CameraController] 필수 매니저를 찾을 수 없습니다.");
            return;
        }

        _shaker.Initialize(cameraTransform);
        _actionView.Initialize(transform, boomTransform, shakeHolderTransform);

        _turnManager.OnTurnStarted += HandleTurnStarted;
        inputManager.OnCameraRecenter += HandleRecenterRequest;

        _isInitialized = true;
        Debug.Log("[CameraController] Initialized & Components Linked.");
        await UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        if (_isInitialized)
        {
            if (_turnManager != null) _turnManager.OnTurnStarted -= HandleTurnStarted;
            if (inputManager != null) inputManager.OnCameraRecenter -= HandleRecenterRequest;
        }

        if (ServiceLocator.IsRegistered<CameraController>())
            ServiceLocator.Unregister<CameraController>(ManagerScope.Scene);
    }

    private void LateUpdate()
    {
        if (_actionView.IsActive)
        {
            _actionView.UpdateView();
        }
        else
        {
            HandleNormalView();
        }
    }

    private void HandleNormalView()
    {
        if (inputManager == null) return;

        Vector2 moveInput = inputManager.CameraMoveVector;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion heading = Quaternion.Euler(0, currentYaw, 0);
            Vector3 forward = heading * Vector3.forward;
            Vector3 right = heading * Vector3.right;
            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;

            targetPosition += moveDir * moveSpeed * Time.deltaTime;
        }

        if (inputManager.IsCameraRotatePressed())
            currentYaw += inputManager.GetMouseDelta().x * mouseRotationSpeed;

        float keyRotation = inputManager.CameraKeyRotateAxis;
        if (Mathf.Abs(keyRotation) > 0.01f)
            currentYaw += keyRotation * rotationSpeed * Time.deltaTime;

        ApplyNormalTransform();
    }

    private void ApplyNormalTransform()
    {
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);

        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        boomTransform.rotation = Quaternion.Slerp(boomTransform.rotation, targetRotation, Time.deltaTime * smoothing);

        if (shakeHolderTransform != null)
        {
            shakeHolderTransform.localPosition = Vector3.Lerp(
                shakeHolderTransform.localPosition,
                GetShakeHolderLocalPos(),
                Time.deltaTime * smoothing
            );
        }
    }

    // --- 외부 제어 메서드 ---

    // [New] 위치와 각도를 모두 초기화하며 즉시 이동 (Recenter 기능)
    public void RecenterCamera(Vector3 position)
    {
        // 1. 각도 리셋
        currentPitch = defaultPitch;
        currentYaw = defaultYaw;

        // 2. 즉시 이동 (Warp) - Zoom도 리셋
        WarpToTarget(position, resetZoom: true);
    }

    // 기존 Warp: 각도 유지, 위치만 즉시 이동
    public void WarpToTarget(Vector3 position, bool resetZoom = true)
    {
        if (_actionView.IsActive) _actionView.Disable();

        targetPosition = position;
        _shaker.Stop();

        transform.position = position;

        if (boomTransform != null)
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        if (resetZoom && shakeHolderTransform != null)
            shakeHolderTransform.localPosition = GetShakeHolderLocalPos();
    }

    public void SetTarget(Transform target, bool immediate = false)
    {
        if (target == null) return;

        if (immediate)
        {
            WarpToTarget(target.position);
        }
        else
        {
            targetPosition = target.position;
        }
    }

    public void PlayImpactShake() => _shaker.Play(shakeDuration, shakeMagnitude);

    public void EnableActionView(Transform attacker, Transform target)
    {
        _shaker.Stop();
        _actionView.Enable(attacker, target);
    }

    public void DisableActionView()
    {
        _actionView.Disable();
        targetPosition = transform.position;
        _shaker.Stop();
    }

    public void ToggleDebugActionView()
    {
        if (_actionView.IsActive) DisableActionView();
        else if (debugAttacker != null && debugTarget != null) EnableActionView(debugAttacker, debugTarget);
    }

    private void HandleTurnStarted(UnitStatus unit)
    {
        if (unit != null) SetTarget(unit.transform, false);
    }

    private void HandleRecenterRequest()
    {
        // [Fix] '8'번 키를 누르면 각도까지 포함하여 완전 초기화
        if (_turnManager != null && _turnManager.ActiveUnit != null)
        {
            RecenterCamera(_turnManager.ActiveUnit.transform.position);
        }
    }
}