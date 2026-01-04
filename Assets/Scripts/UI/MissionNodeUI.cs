using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MissionNodeUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button _button;
    // [New] 숨겼다 보여줄 상세 정보 그룹 (Content_V-Group 연결)
    [SerializeField] private GameObject _contentGroup;

    [Header("Main Info")]
    [SerializeField] private TextMeshProUGUI _missionNameText;
    [SerializeField] private TextMeshProUGUI _locationText;

    [Header("Meta Info")]
    [SerializeField] private TextMeshProUGUI _typeText;
    [SerializeField] private TextMeshProUGUI _turnLimitText;
    [SerializeField] private TextMeshProUGUI _difficultyText;

    [Header("Stats Grid")]
    [SerializeField] private TextMeshProUGUI _mapSizeText;
    [SerializeField] private TextMeshProUGUI _enemyCountText;
    [SerializeField] private TextMeshProUGUI _squadSizeText;

    private Action<MissionDataSO> _onClickCallback;
    private MissionDataSO _currentData;

    private void Awake()
    {
        Debug.Assert(_button != null, "MissionNodeUI: Button is missing.");
        Debug.Assert(_missionNameText != null, "MissionNodeUI: Name Text missing.");
        Debug.Assert(_locationText != null, "MissionNodeUI: Location Text missing.");
        Debug.Assert(_typeText != null, "MissionNodeUI: Type Text missing.");
        Debug.Assert(_difficultyText != null, "MissionNodeUI: Difficulty Text missing.");

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);

            // ====================================================================
            // [New] Hover 이벤트 동적 연결 (EventTrigger)
            // ====================================================================
            // 버튼 게임오브젝트에 EventTrigger가 없으면 추가합니다.
            EventTrigger trigger = _button.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) trigger = _button.gameObject.AddComponent<EventTrigger>();

            // 1. 마우스 진입 (PointerEnter) -> 켜기
            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { SetContentGroupActive(true); });
            trigger.triggers.Add(entryEnter);

            // 2. 마우스 탈출 (PointerExit) -> 끄기
            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { SetContentGroupActive(false); });
            trigger.triggers.Add(entryExit);
        }

        // [New] 시작할 때는 꺼두기
        SetContentGroupActive(false);
    }
    private void SetContentGroupActive(bool isActive)
    {
        if (_contentGroup != null)
        {
            _contentGroup.SetActive(isActive);
        }
    }


    private void HandleClick()
    {
        if (_currentData != null)
        {
            _onClickCallback?.Invoke(_currentData);
        }
    }

    // [Change] MissionDifficulty -> float difficulty
    public void Bind(MissionDataSO mission, float difficulty, Action<MissionDataSO> onClick)
    {
        if (mission == null) return;
        _currentData = mission;
        _onClickCallback = onClick;

        _missionNameText.text = mission.Definition.MissionName;
        _locationText.text = string.IsNullOrEmpty(mission.UI.LocationName) ? "Unknown Area" : mission.UI.LocationName;
        _typeText.text = mission.Definition.Type.ToString();

        int limit = mission.Definition.TimeLimit;
        _turnLimitText.text = limit <= 0 ? "∞ Turns" : $"{limit} Turns";

        // Bind 시점에도 일단 내용은 꺼두는 것이 안전함
        SetContentGroupActive(false);

        // 데이터 바인딩
        _mapSizeText.text = mission.UI.MapSize.ToString();
        _enemyCountText.text = $"Enemy: {mission.UI.EstimatedEnemyCount}";
        _squadSizeText.text = $"Squad: {mission.UI.MaxSquadSize}";
    }


    public void SetInteractable(bool state)
    {
        if (_button != null) _button.interactable = state;
    }
}