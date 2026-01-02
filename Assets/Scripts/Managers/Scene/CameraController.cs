using UnityEngine;
using Cysharp.Threading.Tasks;

public class CameraController : MonoBehaviour, IInitializable
{
    [Header("Settings")]
    //[SerializeField] private float _moveSpeed = 5f;        // 카메라 추적 속도
    [SerializeField] private float _smoothTime = 0.2f;     // 부드러움 정도 (낮을수록 빠름)

    [Header("View Config")]
    [SerializeField] private float _height = 10.0f;        // 요청하신 높이
    [SerializeField] private float _distance = 10.0f;      // 거리
    [SerializeField] private float _angle = 45.0f;         // 요청하신 각도

    private Camera _mainCamera;
    private Transform _cameraTransform;
    private Transform _target; // 추적 대상 (유닛)

    private Vector3 _currentVelocity; // SmoothDamp용
    private Vector3 _targetOffset;

    private void Awake()
    {
        // 중복 방지
        if (ServiceLocator.IsRegistered<CameraController>())
        {
            Destroy(gameObject);
            return;
        }
        ServiceLocator.Register(this, ManagerScope.Scene);

        _mainCamera = GetComponent<Camera>();
        if (_mainCamera == null) _mainCamera = Camera.main;
        _cameraTransform = transform;
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<CameraController>())
            ServiceLocator.Unregister<CameraController>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 45도 뷰를 위한 오프셋 설정
        _targetOffset = new Vector3(0, _height, -_distance);

        // 카메라 각도 강제 설정
        _cameraTransform.rotation = Quaternion.Euler(_angle, 0, 0);

        Debug.Log("[CameraController] Initialized.");
        await UniTask.CompletedTask;
    }

    // PlayerController가 호출함
    public void SetTarget(Transform target)
    {
        _target = target;
        if (_target != null)
        {
            Debug.Log($"[CameraController] Now following: {_target.name}");
            // 타겟 변경 시 즉시 근처로 이동 (너무 멀면 튀니까)
            Vector3 desiredPos = _target.position + _targetOffset;
            if (Vector3.Distance(_cameraTransform.position, desiredPos) > 50f)
            {
                _cameraTransform.position = desiredPos;
            }
        }
    }

    private void LateUpdate()
    {
        if (_target != null)
        {
            FollowTarget();
        }
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = _target.position + _targetOffset;

        // SmoothDamp: 부드럽게 감속하며 따라감
        _cameraTransform.position = Vector3.SmoothDamp(
            _cameraTransform.position,
            desiredPosition,
            ref _currentVelocity,
            _smoothTime
        );
    }
}