using UnityEngine;

public class CameraActionView : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private Vector3 _actionViewOffset = new Vector3(0.5f, 1.8f, -2.0f);
    [SerializeField] private float _targetHeightOffset = 1.0f;

    // 제어할 트랜스폼 참조
    private Transform _rootTransform;
    private Transform _boomTransform;
    private Transform _shakeHolderTransform;

    // 상태 데이터
    private Transform _attacker;
    private Transform _target;

    public bool IsActive { get; private set; }

    public void Initialize(Transform root, Transform boom, Transform shakeHolder)
    {
        _rootTransform = root;
        _boomTransform = boom;
        _shakeHolderTransform = shakeHolder;
        IsActive = false;
    }

    public void Enable(Transform attacker, Transform target)
    {
        if (attacker == null || target == null) return;

        _attacker = attacker;
        _target = target;
        IsActive = true;

        UpdateView(); // 활성화 즉시 위치 갱신
    }

    public void Disable()
    {
        IsActive = false;
        _attacker = null;
        _target = null;
    }

    // 매 프레임 호출되어 위치를 갱신합니다.
    public void UpdateView()
    {
        if (!IsActive || _attacker == null || _target == null)
        {
            Disable();
            return;
        }

        // 1. 타겟 방향 계산
        Vector3 directionToTarget = (_target.position - _attacker.position).normalized;
        if (directionToTarget == Vector3.zero) directionToTarget = _attacker.forward;

        // 2. 우측 방향 계산
        Vector3 rightDir = Vector3.Cross(Vector3.up, directionToTarget).normalized;

        // 3. 카메라 위치 계산 (Attacker 기준 오프셋 적용)
        Vector3 targetPos = _attacker.position
                            + (rightDir * _actionViewOffset.x)
                            + (Vector3.up * _actionViewOffset.y)
                            + (directionToTarget * _actionViewOffset.z);

        // 4. 회전 계산 (Target의 특정 높이를 바라봄)
        Vector3 lookAtPoint = _target.position + (Vector3.up * _targetHeightOffset);
        Quaternion targetRot = Quaternion.LookRotation(lookAtPoint - targetPos);

        // 5. 적용
        if (_rootTransform != null) _rootTransform.position = targetPos;
        if (_boomTransform != null) _boomTransform.rotation = targetRot;

        // 6. 줌(ShakeHolder) 초기화 (액션 뷰에서는 줌인 상태 제거)
        if (_shakeHolderTransform != null)
        {
            _shakeHolderTransform.localPosition = Vector3.zero;
            _shakeHolderTransform.localRotation = Quaternion.identity;
        }
    }
}