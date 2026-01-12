using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class Projectile : MonoBehaviour
{
    [Header("Visual References")]
    [SerializeField] private TrailRenderer _trail;
    [SerializeField] private ParticleSystem _hitVFX;
    [SerializeField] private GameObject _meshObject;

    [Header("Settings")]
    [Tooltip("피격 이펙트 재생 시간(초) - 이 시간만큼 대기 후 투사체가 반환됩니다.")]
    [SerializeField] private float _vfxDuration = 1.0f; // [Fix 1] 하드코딩/프로퍼티 접근 대신 명시적 설정

    private bool _isActive = false;
    private CancellationTokenSource _cts;

    private void OnEnable()
    {
        _cts = new CancellationTokenSource();
        // [Fix 2] OnEnable에서 _isActive = true 제거 (LaunchAsync 시작 시 활성화)
        if (_trail != null) _trail.Clear();
        if (_meshObject != null) _meshObject.SetActive(true);
    }

    private void OnDisable()
    {
        CancelTask();
    }

    private void OnDestroy()
    {
        CancelTask();
    }

    private void CancelTask()
    {
        _isActive = false;
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    public async UniTask LaunchAsync(Vector3 start, Vector3 end, float speed, Action onHitCallback = null)
    {
        _isActive = true; // [Fix 2] 발사 시점에 활성화

        transform.position = start;

        // 목표 방향 회전
        Vector3 direction = (end - start).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        float distance = Vector3.Distance(start, end);
        float duration = (speed > 0) ? distance / speed : 0f;
        float elapsed = 0f;

        var token = _cts.Token;

        try
        {
            while (elapsed < duration)
            {
                if (token.IsCancellationRequested || !_isActive) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                transform.position = Vector3.Lerp(start, end, t);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            transform.position = end;

            // 타격 시점 콜백
            onHitCallback?.Invoke();

            // [Fix 3] VFX 및 메쉬 처리
            if (_hitVFX != null)
            {
                _hitVFX.Play();
                // 이펙트 시작 직후 메쉬 숨김
                if (_meshObject != null) _meshObject.SetActive(false);

                // [Fix 1] 설정된 시간만큼 대기
                await UniTask.Delay(Mathf.CeilToInt(_vfxDuration * 1000), cancellationToken: token);
            }
        }
        catch (OperationCanceledException)
        {
            // 작업 취소됨
        }
        catch (Exception ex)
        {
            Debug.LogError($"[Projectile] Error: {ex.Message}");
        }
    }

    public void Deactivate()
    {
        _isActive = false;
        gameObject.SetActive(false);
    }
}