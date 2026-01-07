using UnityEngine;
using Cysharp.Threading.Tasks;

public class Projectile : MonoBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("총알의 꼬리 효과 (LineRenderer 기반)")]
    [SerializeField] private TrailRenderer _trail;
    [Tooltip("목표 도달 시 터질 이펙트")]
    [SerializeField] private ParticleSystem _hitVFX;

    private void Awake()
    {
        // TrailRenderer가 있다면 자동으로 설정
        if (_trail == null) _trail = GetComponent<TrailRenderer>();
    }

    /// <summary>
    /// 시작점에서 목표 지점까지 직선으로 등속 이동합니다.
    /// </summary>
    public async UniTask LaunchAsync(Vector3 startPos, Vector3 targetPos, float speed)
    {
        // 1. 초기 위치 설정
        transform.position = startPos;

        // 2. 방향 설정 (루프 진입 전 1회만 계산 - 직선탄 최적화)
        Vector3 direction = (targetPos - startPos).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        gameObject.SetActive(true);

        // 잔상(Trail) 초기화
        if (_trail != null) _trail.Clear();

        // 3. 이동 루프 (회전 연산 제거됨)
        // 목표 지점과의 거리가 매우 가까워질 때까지 이동
        while (this != null && Vector3.SqrMagnitude(transform.position - targetPos) > 0.0025f) // 0.05 * 0.05
        {
            // MoveTowards: 현재 위치에서 목표 위치로 직선 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

            await UniTask.NextFrame();
        }

        // 4. 도착 처리
        if (this != null)
        {
            transform.position = targetPos;

            // 피격 이펙트 재생
            if (_hitVFX != null)
            {
                _hitVFX.Play();
            }

            // 이펙트가 보일 수 있도록 잠시 대기 후 비활성화 (선택 사항)
            // 여기서는 0.5초 뒤에 끕니다.
            await UniTask.Delay(500);
            gameObject.SetActive(false);
        }
    }
}