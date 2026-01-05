using UnityEngine;
using System.Collections;
using Cysharp.Threading.Tasks; // [필수] IInitializable 및 UniTask 사용

// [변경] IInitializable 인터페이스 구현 추가
public class CameraController : MonoBehaviour, IInitializable
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float mouseRotationSpeed = 0.2f;
    [SerializeField] private float smoothing = 5f;

    [Header("Rotation Defaults")]
    [SerializeField] private float defaultPitch = 45f;
    [SerializeField] private float defaultYaw = 45f; // [권장] 45도로 설정하여 쿼터뷰 보장

    [Header("Zoom Settings")]
    [Tooltip("카메라의 고정 줌 거리")]
    [SerializeField] private float fixedZoomDistance = 14f; // [권장] 10 -> 14로 약간 멀리

    [Header("Action View Settings")]
    [SerializeField] private Vector3 actionViewOffset = new Vector3(0.5f, 1.8f, -2.0f);
    [SerializeField] private float targetHeightOffset = 1.0f;

    [Header("Debug Settings")]
    [SerializeField] private Transform debugAttacker;
    [SerializeField] private Transform debugTarget;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f;

    // --- 내부 계층 구조 ---
    private Transform boomTransform;      // 회전 담당 (Pitch/Yaw)
    private Transform shakeHolderTransform; // 줌 담당 (Zoom Distance)
    private Transform cameraTransform;    // 실제 카메라 & 쉐이크 담당 (Shake)

    private InputManager inputManager;
    private Vector3 targetPosition;

    [SerializeField] private float currentYaw;
    [SerializeField] private float currentPitch;

    // --- 액션 뷰 상태 ---
    private bool isActionView = false;
    private Transform actionAttacker;
    private Transform actionTarget;

    // 쉐이크 코루틴 관리
    private Coroutine currentShakeCoroutine;

    private void Awake()
    {
        // [삭제됨] 여기서 등록하면 중복 오류 발생함
        // ServiceLocator.Register(this, ManagerScope.Scene); 

        // 1. 실제 카메라 찾기
        Camera cam = GetComponentInChildren<Camera>();
        if (cam == null)
        {
            Debug.LogError("[CameraController] 자식 계층에 Camera가 없습니다!");
            enabled = false;
            return;
        }
        cameraTransform = cam.transform;

        // 2. CameraBoom 생성 또는 찾기
        boomTransform = transform.Find("CameraBoom");
        if (boomTransform == null)
        {
            GameObject boomObj = new GameObject("CameraBoom");
            boomTransform = boomObj.transform;
            boomTransform.SetParent(transform, false);
        }

        // 3. ShakeHolder (Zoom 담당) 생성 또는 찾기
        shakeHolderTransform = boomTransform.Find("ShakeHolder");
        if (shakeHolderTransform == null)
        {
            GameObject holderObj = new GameObject("ShakeHolder");
            shakeHolderTransform = holderObj.transform;
            shakeHolderTransform.SetParent(boomTransform, false);
        }

        // 4. 계층 구조 정리: Root -> Boom -> ShakeHolder -> Camera
        if (cameraTransform.parent != shakeHolderTransform)
        {
            cameraTransform.SetParent(shakeHolderTransform);
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }

        targetPosition = transform.position;
    }

    // [변경] SceneInitializer가 호출하는 초기화 함수
    public async UniTask Initialize(InitializationContext context)
    {
        // 1. 서비스 등록 (순서 보장)
        ServiceLocator.Register(this, ManagerScope.Scene);

        // 2. 의존성 주입
        inputManager = ServiceLocator.Get<InputManager>();

        // 3. [중요] 초기 각도 및 줌 강제 설정 (백뷰 방지)
        currentPitch = defaultPitch;
        currentYaw = defaultYaw;

        if (boomTransform != null)
        {
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        }

        if (shakeHolderTransform != null)
        {
            shakeHolderTransform.localPosition = new Vector3(0, 0, -fixedZoomDistance);
        }

        Debug.Log($"[CameraController] Initialized. Pitch:{currentPitch}, Yaw:{currentYaw}, Dist:{fixedZoomDistance}");

        await UniTask.CompletedTask;
    }

    // [삭제] Start() 제거 -> Initialize()로 통합됨
    /*
    private void Start()
    {
        inputManager = ServiceLocator.Get<InputManager>();
    }
    */

    private void LateUpdate()
    {
        if (isActionView)
        {
            if (actionAttacker != null && actionTarget != null)
            {
                HandleActionView();
            }
        }
        else
        {
            HandleNormalView();
        }
    }

    private void HandleNormalView()
    {
        if (inputManager == null) return;

        // --- 이동 ---
        Vector2 moveInput = inputManager.CameraMoveVector;
        if (moveInput.sqrMagnitude > 0.01f)
        {
            Quaternion heading = Quaternion.Euler(0, currentYaw, 0);
            Vector3 forward = heading * Vector3.forward;
            Vector3 right = heading * Vector3.right;
            Vector3 moveDir = (forward * moveInput.y + right * moveInput.x).normalized;
            targetPosition += moveDir * moveSpeed * Time.deltaTime;
        }

        // --- 회전 ---
        if (inputManager.IsCameraRotatePressed())
        {
            Vector2 mouseDelta = inputManager.GetMouseDelta();
            currentYaw += mouseDelta.x * mouseRotationSpeed;
        }

        float keyRotation = inputManager.CameraKeyRotateAxis;
        if (Mathf.Abs(keyRotation) > 0.01f)
        {
            currentYaw += keyRotation * rotationSpeed * Time.deltaTime;
        }

        ApplyNormalTransform();
    }

    private void ApplyNormalTransform()
    {
        // 1. Root 이동 (TargetPosition은 카메라가 바라보는 '중심점'입니다)
        Vector3 nextPos = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);
        transform.position = nextPos;

        // 2. Boom 회전
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        boomTransform.rotation = Quaternion.Slerp(boomTransform.rotation, targetRotation, Time.deltaTime * smoothing);

        // 3. Zoom (ShakeHolder)
        if (shakeHolderTransform != null)
        {
            Vector3 targetLocalPos = new Vector3(0, 0, -fixedZoomDistance);
            shakeHolderTransform.localPosition = Vector3.Lerp(shakeHolderTransform.localPosition, targetLocalPos, Time.deltaTime * smoothing);
        }
    }

    public void SetTarget(Transform target, bool immediate = false)
    {
        if (target == null)
        {
            Debug.LogError("[DEBUG_CAM] SetTarget : Target Null입니다.");
            return;
        }

        if (isActionView) DisableActionView();

        targetPosition = target.position;

        // [중요] 각도 초기화 코드 제거됨 (플레이어가 돌린 각도 유지)
        // currentPitch = defaultPitch; 
        // currentYaw = defaultYaw; 

        // 줌 거리 초기화 (필요시 주석 처리 가능)
        if (shakeHolderTransform != null)
        {
            shakeHolderTransform.localPosition = new Vector3(0, 0, -fixedZoomDistance);
        }

        StopShake();

        if (immediate)
        {
            Debug.Log("<color=cyan>[DEBUG_CAM] 즉시 이동 실행</color>");
            transform.position = targetPosition;
            // 즉시 이동 시에는 현재 각도 반영
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        }
    }

    public void EnableActionView(Transform attacker, Transform target)
    {
        if (cameraTransform == null || attacker == null || target == null) return;

        actionAttacker = attacker;
        actionTarget = target;
        isActionView = true;

        StopShake();
        HandleActionView();
    }

    public void DisableActionView()
    {
        isActionView = false;
        actionAttacker = null;
        actionTarget = null;
        targetPosition = transform.position;

        StopShake();
    }

    private void HandleActionView()
    {
        if (actionAttacker == null || actionTarget == null)
        {
            DisableActionView();
            return;
        }

        Vector3 directionToTarget = (actionTarget.position - actionAttacker.position).normalized;
        if (directionToTarget == Vector3.zero) directionToTarget = actionAttacker.forward;

        Vector3 rightDir = Vector3.Cross(Vector3.up, directionToTarget).normalized;

        Vector3 targetPos = actionAttacker.position
                            + (rightDir * actionViewOffset.x)
                            + (Vector3.up * actionViewOffset.y)
                            + (directionToTarget * actionViewOffset.z);

        Vector3 lookAtPoint = actionTarget.position + (Vector3.up * targetHeightOffset);
        Quaternion targetRot = Quaternion.LookRotation(lookAtPoint - targetPos);

        transform.position = targetPos;
        boomTransform.rotation = targetRot;

        if (shakeHolderTransform != null)
        {
            shakeHolderTransform.localPosition = Vector3.zero;
            shakeHolderTransform.localRotation = Quaternion.identity;
        }
    }

    public void ToggleDebugActionView()
    {
        if (isActionView) DisableActionView();
        else if (debugAttacker != null && debugTarget != null) EnableActionView(debugAttacker, debugTarget);
        else Debug.LogWarning("[CameraController] 디버그 타겟이 없습니다.");
    }

    // --- Shake 기능 ---

    public void PlayImpactShake()
    {
        StopShake();
        currentShakeCoroutine = StartCoroutine(Co_ImpactShake(shakeDuration, shakeMagnitude));
    }

    private void StopShake()
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            currentShakeCoroutine = null;
        }
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = Vector3.zero;
        }
    }

    private IEnumerator Co_ImpactShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float damping = 1.0f - (elapsed / duration);
            float currentMag = magnitude * damping;

            float x = Random.Range(-1f, 1f) * currentMag;
            float y = Random.Range(-1f, 1f) * currentMag;

            cameraTransform.localPosition = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = Vector3.zero;
        currentShakeCoroutine = null;
    }
}