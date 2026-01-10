using UnityEngine;
using TMPro;
using UnityEngine.EventSystems; // [필수] 이벤트를 쓰기 위해 추가

// [수정] 인터페이스 2개 추가 (IPointerDownHandler, IPointerUpHandler)
public class TimelineSlot : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI Component")]
    [Tooltip("유닛 이름이 표시될 텍스트")]
    [SerializeField] private TextMeshProUGUI _nameText;

    private UnitStatus _targetUnit;
    private RectTransform _rectTransform;

    public UnitStatus GetTargetUnit() => _targetUnit;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void Bind(UnitStatus unit)
    {
        if (unit == null || unit.unitData == null)
        {
            gameObject.SetActive(false);
            return;
        }

        _targetUnit = unit;
        gameObject.SetActive(true);

        if (_nameText != null)
        {
            _nameText.text = unit.unitData.UnitName;
        }
    }

    public void UpdatePositionOnGauge(float maxTS, float gaugeHeight, float xOffset)
    {
        if (_targetUnit == null || _targetUnit.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        // TS 0 = 천장(비율 1), TS Max = 바닥(비율 0)
        float currentTS = Mathf.Clamp(_targetUnit.CurrentTS, 0f, maxTS);
        float ratio = 1f - (currentTS / maxTS);

        // [수정] 피벗이 바닥(0)일 경우, 그냥 높이 * 비율이 곧 Y좌표입니다.
        // 뒤에 붙어있던 "- (gaugeHeight * 0.5f)"를 지우세요.
        float yPos = ratio * gaugeHeight;

        // 조금 더 예쁘게 하려면 (슬롯 크기 반만큼 안쪽으로)
        // float yPos = ratio * (gaugeHeight - 슬롯높이) + (슬롯높이/2); 
        // 하지만 일단 위 코드로 먼저 위치부터 잡으세요.

        _rectTransform.anchoredPosition = new Vector2(xOffset, yPos);
    }

    // --- [추가된 기능] ---

    // 1. 꾹 눌렀을 때
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_targetUnit == null) return;

        // ServiceLocator를 통해 카메라 컨트롤러를 찾습니다.
        if (ServiceLocator.TryGet<CameraController>(out var cam))
        {
            cam.SavePosition(); // 1. 현재 위치 저장
            cam.SetTarget(_targetUnit.transform); // 2. 유닛 비추기
        }
    }

    // 2. 손을 뗐을 때
    public void OnPointerUp(PointerEventData eventData)
    {
        if (ServiceLocator.TryGet<CameraController>(out var cam))
        {
            cam.RestorePosition(); // 3. 원래 위치로 복귀
        }
    }
}