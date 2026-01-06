using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AbilityRangeVisualizer : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField, Range(12, 360)] private int _segments = 60;
    [SerializeField] private float _heightOffset = 0.1f;

    // 명시적 머티리얼 할당 권장
    [SerializeField] private Material _lineMaterial;

    private LineRenderer _lineRenderer;
    private bool _initialized = false;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        // 1. 초기 안전 설정
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = false; // 코드로 닫으므로 false
        _lineRenderer.enabled = false;

        // 2. 머티리얼 방어 코드 (피드백 반영)
        // Inspector에서 할당을 깜빡했더라도, 눈에 띄는 색으로 표시하여 수정 유도
        if (_lineMaterial != null)
        {
            _lineRenderer.material = _lineMaterial;
        }
        else if (_lineRenderer.material == null)
        {
            Debug.LogWarning($"[{nameof(AbilityRangeVisualizer)}] Material is missing! Using default debug material.");
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default")) { color = Color.red };
        }

        // 3. 라인 폭 0 방지 (피드백 반영)
        if (_lineRenderer.widthMultiplier == 0f)
        {
            _lineRenderer.widthMultiplier = 0.05f;
        }
    }

    /// <summary>
    /// 사거리를 설정하고 라인을 그립니다. (여러 번 호출 시 갱신됨)
    /// </summary>
    /// <param name="range">반경 (0 이하시 꺼짐)</param>
    public void Setup(float range)
    {
        if (_lineRenderer == null) return;

        // 유효성 검사
        if (range <= 0)
        {
            Hide();
            return;
        }

        // 원을 완벽히 닫기 위해 점 개수 + 1
        int pointCount = _segments + 1;
        _lineRenderer.positionCount = pointCount;

        float angleStep = 360f / _segments;

        for (int i = 0; i <= _segments; i++)
        {
            float angleRad = Mathf.Deg2Rad * (angleStep * i);
            float x = Mathf.Sin(angleRad) * range;
            float z = Mathf.Cos(angleRad) * range;

            _lineRenderer.SetPosition(i, new Vector3(x, _heightOffset, z));
        }

        _initialized = true;
    }

    public void Show()
    {
        if (_lineRenderer == null) return;

        if (!_initialized)
        {
            Debug.LogWarning($"[{nameof(AbilityRangeVisualizer)}] Show() called before Setup(). Call ignored.");
            return;
        }

        _lineRenderer.enabled = true;
    }

    public void Hide()
    {
        if (_lineRenderer != null)
            _lineRenderer.enabled = false;
    }
}