using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QTEUIController : MonoBehaviour
{
    [Header("Root Panel")]
    [SerializeField] private RectTransform _rootPanel;

    [Header("Zones (Explicit Binding)")]
    [SerializeField] private RectTransform _greenRect;  // Graze
    [SerializeField] private RectTransform _yellowRect; // Hit
    [SerializeField] private RectTransform _redRect;    // Critical

    [Header("Needle")]
    [SerializeField] private RectTransform _needleRect;
    [SerializeField] private float _needleWidth = 4f;

    [Header("Feedback")]
    [SerializeField] private TextMeshProUGUI _verdictText;
    [SerializeField] private float _verdictDuration = 1.5f;

    private float _verdictTimer = 0f;

    private void Awake()
    {
        SetVisible(false);
    }

    // ------------------------------------------------------------------------
    // 라이프사이클 관리
    // ------------------------------------------------------------------------
    public void StartQTE()
    {
        SetVisible(true);
        _verdictTimer = 0f;
        if (_verdictText != null) _verdictText.gameObject.SetActive(false);
        UpdateNeedle(0f);
    }

    public void EndQTE()
    {
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (_rootPanel != null) _rootPanel.gameObject.SetActive(visible);
        else gameObject.SetActive(visible);
    }

    // ------------------------------------------------------------------------
    // 타이머 로직
    // ------------------------------------------------------------------------
    private void Update()
    {
        if (_verdictTimer > 0f)
        {
            _verdictTimer -= Time.deltaTime;
            if (_verdictTimer <= 0f)
            {
                if (_verdictText != null) _verdictText.gameObject.SetActive(false);
            }
        }
    }

    // ------------------------------------------------------------------------
    // UI 설정
    // ------------------------------------------------------------------------
    public void SetupZones(ZonesContainer zones)
    {
        ApplyZoneToRect(_greenRect, zones.Graze);
        ApplyZoneToRect(_yellowRect, zones.Hit);
        ApplyZoneToRect(_redRect, zones.Critical);

        // 시각적 순서 보정 (Green -> Yellow -> Red -> Needle)
        if (_greenRect) _greenRect.SetAsLastSibling();
        if (_yellowRect) _yellowRect.SetAsLastSibling();
        if (_redRect) _redRect.SetAsLastSibling();
        if (_needleRect) _needleRect.SetAsLastSibling();
    }

    private void ApplyZoneToRect(RectTransform rect, ZoneInfo zone)
    {
        if (rect == null) return;

        if (zone.IsValid)
        {
            rect.gameObject.SetActive(true);
            rect.anchorMin = new Vector2(zone.StartMin, 0f);
            rect.anchorMax = new Vector2(zone.EndMax, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.gameObject.SetActive(false);
        }
    }

    public void UpdateNeedle(float value01)
    {
        if (_needleRect == null) return;

        _needleRect.anchorMin = new Vector2(value01, 0f);
        _needleRect.anchorMax = new Vector2(value01, 1f);
        _needleRect.anchoredPosition = Vector2.zero;

        if (_needleWidth > 0)
            _needleRect.sizeDelta = new Vector2(_needleWidth, 0f);
    }

    public void ShowVerdict(QTEGrade grade)
    {
        if (_verdictText == null) return;

        _verdictText.gameObject.SetActive(true);
        _verdictText.text = grade.ToString().ToUpper();
        _verdictText.color = QTEMath.GetZoneColor(grade);

        _verdictTimer = _verdictDuration;
    }

    [ContextMenu("Debug/Test UI Layout (Hit/Crit)")]
    private void DebugTestUI()
    {
        // 1. 가짜 데이터 생성 (Graze: 30~70%, Hit: 45~55%, Crit: 48~52%)
        var graze = new ZoneInfo(QTEGrade.Graze, 0.30f, 0.70f);
        var hit = new ZoneInfo(QTEGrade.Hit, 0.45f, 0.55f);
        var crit = new ZoneInfo(QTEGrade.Critical, 0.48f, 0.52f);

        var fakeZones = new ZonesContainer(graze, hit, crit);

        // 2. 메서드 실행
        Debug.Log("[UI Test] StartQTE -> SetupZones -> UpdateNeedle(0.5) -> ShowVerdict");

        StartQTE();              // 1. UI 켜기 및 초기화
        SetupZones(fakeZones);   // 2. 영역 배치
        UpdateNeedle(0.5f);      // 3. 바늘을 정중앙에 배치
        ShowVerdict(QTEGrade.Critical); // 4. 결과 텍스트 출력 테스트
    }
}