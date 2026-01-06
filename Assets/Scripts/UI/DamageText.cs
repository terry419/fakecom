using UnityEngine;
using TMPro;
using Cysharp.Threading.Tasks;
using UnityEngine.Pool;
using System.Threading;

public class DamageText : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TextMeshProUGUI _textMesh; // UI 텍스트로 변경

    [Header("Animation Settings")]
    [SerializeField] private float _moveDistance = 2.0f;
    [SerializeField] private float _duration = 1.0f;
    [SerializeField] private AnimationCurve _fadeCurve;

    private IObjectPool<DamageText> _managedPool;
    private Camera _targetCamera;
    private CancellationTokenSource _cts;

    private void Awake()
    {
        // 런타임에 유연하게 대응하기 위해 GetComponent 사용
        if (_textMesh == null) _textMesh = GetComponentInChildren<TextMeshProUGUI>();

        if (_fadeCurve.length == 0)
            _fadeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    }

    public void SetPool(IObjectPool<DamageText> pool) => _managedPool = pool;

    private void OnEnable()
    {
        // Camera.main 캐싱 (성능 최적화)
        _targetCamera = Camera.main;
        _cts = new CancellationTokenSource();
    }

    private void OnDisable()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    /// <summary>
    /// 풀 반환 시 상태 완전 초기화
    /// </summary>
    public void ResetState()
    {
        _cts?.Cancel();
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        if (_textMesh != null)
        {
            _textMesh.text = "";
            _textMesh.color = Color.white;
        }
    }

    public void Play(int damage, Color color, float scale, bool isMiss)
    {
        transform.localScale = Vector3.one * scale;
        _textMesh.color = color;
        _textMesh.text = isMiss ? "MISS" : damage.ToString();
        if (!isMiss && scale > 1.2f) _textMesh.text += "!";

        AnimateRoutine(_cts.Token).Forget();
    }

    private async UniTaskVoid AnimateRoutine(CancellationToken token)
    {
        float timer = 0f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + Vector3.up * _moveDistance;
        Color startColor = _textMesh.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f); // 타겟 알파는 0

        try
        {
            while (timer < _duration)
            {
                if (token.IsCancellationRequested) return;

                timer += Time.deltaTime;
                float progress = timer / _duration;

                // 1. 위로 이동
                transform.position = Vector3.Lerp(startPos, endPos, progress);

                // 2. 지적하신 Color.Lerp 기반 페이드 적용
                float curveValue = _fadeCurve.Evaluate(progress);
                _textMesh.color = Color.Lerp(endColor, startColor, curveValue);

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        if (_managedPool != null) _managedPool.Release(this);
        else Destroy(gameObject);
    }

    private void LateUpdate()
    {
        // 1. 카메라가 회전해도 항상 정면을 보도록 수정
        if (_targetCamera != null && gameObject.activeInHierarchy)
        {
            // 빌보드: 카메라의 회전값을 그대로 복사 (LookRotation의 역방향 오류 방지)
            transform.rotation = _targetCamera.transform.rotation;
        }
    }
}