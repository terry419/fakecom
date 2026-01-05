using UnityEngine;
using System.Collections; // [수정] IEnumerator 사용을 위해 필수 추가

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private float mouseRotationSpeed = 0.2f;
    [SerializeField] private float smoothing = 5f;

    [Header("Rotation Defaults")]
    [SerializeField] private float defaultPitch = 45f;
    [SerializeField] private float defaultYaw = 0f;

    [Header("Zoom Settings")]
    [Tooltip("카메라의 고정 줌 거리")]
    [SerializeField] private float fixedZoomDistance = 10f;

    [Header("Action View Settings")]
    [SerializeField] private Vector3 actionViewOffset = new Vector3(0.5f, 1.8f, -2.0f);
    [SerializeField] private float targetHeightOffset = 1.0f;

    [Header("Debug Settings")]
    [SerializeField] private Transform debugAttacker;
    [SerializeField] private Transform debugTarget;

    [Header("Shake Settings")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 0.2f; // 조금 더 잘 보이게 기본값 상향

    // --- 내부 계층 구조 ---
    private Transform boomTransform;      // 회전 담당 (Pitch/Yaw)
    private Transform shakeHolderTransform; // [추가] 줌 담당 (Zoom Distance)
    private Transform cameraTransform;    // [수정] 실제 카메라 & 쉐이크 담당 (Shake)

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
        ServiceLocator.Register(this, ManagerScope.Scene);

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

        // 3. [개선] ShakeHolder (Zoom 담당) 생성 또는 찾기
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
            // 카메라는 홀더의 정중앙에 위치 (쉐이크 전)
            cameraTransform.localPosition = Vector3.zero;
            cameraTransform.localRotation = Quaternion.identity;
        }

        targetPosition = transform.position;
        currentPitch = defaultPitch;
        currentYaw = defaultYaw;
    }

    private void Start()
    {
        inputManager = ServiceLocator.Get<InputManager>();
    }

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
        // [DEBUG] 3. 실제 이동 로직 확인 (목표지점과 현재지점이 다를 때만 로그 출력)
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            Debug.Log($"<color=cyan>[DEBUG_CAM] 이동 중... 현재: {transform.position} -> 목표: {targetPosition} (Smoothing: {smoothing})</color>");
        }

        // 1. Root 이동
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothing);

        // 2. Boom 회전
        Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        boomTransform.rotation = Quaternion.Slerp(boomTransform.rotation, targetRotation, Time.deltaTime * smoothing);

        // 3. Zoom
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
            Debug.LogError("[DEBUG_CAM] SetTarget 실패: Target이 Null입니다.");
            return;
        }

        if (isActionView) DisableActionView();

        // [DEBUG] 2. 명령 수신 확인
        Debug.Log($"<color=cyan>[DEBUG_CAM] SetTarget 수신 완료. 대상: {target.name}, 목표 좌표: {target.position}, 즉시이동: {immediate}</color>");

        targetPosition = target.position;
        currentPitch = defaultPitch;
        currentYaw = defaultYaw;

        if (shakeHolderTransform != null)
        {
            shakeHolderTransform.localPosition = new Vector3(0, 0, -fixedZoomDistance);
        }

        StopShake();

        if (immediate)
        {
            Debug.Log("<color=cyan>[DEBUG_CAM] 즉시 이동 실행</color>");
            transform.position = targetPosition;
            boomTransform.rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        }
    }
    public void EnableActionView(Transform attacker, Transform target)
    {
        if (cameraTransform == null || attacker == null || target == null) return;

        actionAttacker = attacker;
        actionTarget = target;
        isActionView = true;

        // 쉐이크 초기화 (액션뷰 진입 시 흔들림 멈춤)
        StopShake();
        HandleActionView();
    }

    public void DisableActionView()
    {
        isActionView = false;
        actionAttacker = null;
        actionTarget = null;
        targetPosition = transform.position;

        // 쉐이크 초기화
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

        // [개선] 액션뷰에서는 ShakeHolder를 0으로 초기화 (줌 없음)
        if (shakeHolderTransform != null)
        {
            shakeHolderTransform.localPosition = Vector3.zero;
            shakeHolderTransform.localRotation = Quaternion.identity;
        }

        // 카메라도 정위치 (쉐이크가 있다면 여기서 오프셋이 추가됨)
        // cameraTransform.localPosition = Vector3.zero; (제거: 쉐이크 로직과 충돌 방지)
    }

    public void ToggleDebugActionView()
    {
        if (isActionView) DisableActionView();
        else if (debugAttacker != null && debugTarget != null) EnableActionView(debugAttacker, debugTarget);
        else Debug.LogWarning("[CameraController] 디버그 타겟이 없습니다.");
    }

    // --- Shake 기능 개선 ---

    public void PlayImpactShake()
    {
        StopShake(); // 기존 쉐이크 중단 및 리셋
        currentShakeCoroutine = StartCoroutine(Co_ImpactShake(shakeDuration, shakeMagnitude));
    }

    private void StopShake()
    {
        if (currentShakeCoroutine != null)
        {
            StopCoroutine(currentShakeCoroutine);
            currentShakeCoroutine = null;
        }
        // 카메라 위치 원상 복구 (ShakeHolder 기준 0)
        if (cameraTransform != null)
        {
            cameraTransform.localPosition = Vector3.zero;
        }
    }

    private IEnumerator Co_ImpactShake(float duration, float magnitude)
    {
        float elapsed = 0.0f;

        // [심화] 감쇠(Damping) 적용: 시간이 갈수록 흔들림이 약해지도록
        while (elapsed < duration)
        {
            float damping = 1.0f - (elapsed / duration); // 1 -> 0 으로 감소
            float currentMag = magnitude * damping;

            float x = Random.Range(-1f, 1f) * currentMag;
            float y = Random.Range(-1f, 1f) * currentMag;

            // [개선] ShakeHolder 아래에서 Camera만 흔들기 때문에 줌 로직과 충돌 안 함
            cameraTransform.localPosition = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraTransform.localPosition = Vector3.zero;
        currentShakeCoroutine = null;
    }
}

