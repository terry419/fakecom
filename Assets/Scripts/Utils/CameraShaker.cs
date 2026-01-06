using UnityEngine;
using System.Collections;

public class CameraShaker : MonoBehaviour
{
    private Transform _cameraTransform;
    private Coroutine _currentShakeCoroutine;

    // 초기화: 실제 흔들릴 대상(카메라)을 받습니다.
    public void Initialize(Transform cameraTransform)
    {
        _cameraTransform = cameraTransform;
    }

    public void Play(float duration, float magnitude)
    {
        Stop(); // 기존 쉐이크가 있다면 중단
        _currentShakeCoroutine = StartCoroutine(Co_Shake(duration, magnitude));
    }

    public void Stop()
    {
        if (_currentShakeCoroutine != null)
        {
            StopCoroutine(_currentShakeCoroutine);
            _currentShakeCoroutine = null;
        }

        // 원점 복구
        if (_cameraTransform != null)
        {
            _cameraTransform.localPosition = Vector3.zero;
        }
    }

    private IEnumerator Co_Shake(float duration, float magnitude)
    {
        float elapsed = 0.0f;
        while (elapsed < duration)
        {
            float damping = 1.0f - (elapsed / duration);
            float currentMag = magnitude * damping;

            float x = Random.Range(-1f, 1f) * currentMag;
            float y = Random.Range(-1f, 1f) * currentMag;

            // 로컬 포지션을 흔듭니다.
            _cameraTransform.localPosition = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        _cameraTransform.localPosition = Vector3.zero;
        _currentShakeCoroutine = null;
    }
}