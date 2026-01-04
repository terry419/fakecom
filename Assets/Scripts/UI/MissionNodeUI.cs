using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class MissionNodeUI : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Button _button;

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
        // [Fix] Null 방어 강화
        Debug.Assert(_button != null, "MissionNodeUI: Button is missing.");
        Debug.Assert(_missionNameText != null, "MissionNodeUI: Name Text missing.");
        Debug.Assert(_locationText != null, "MissionNodeUI: Location Text missing.");
        Debug.Assert(_typeText != null, "MissionNodeUI: Type Text missing.");
        Debug.Assert(_difficultyText != null, "MissionNodeUI: Difficulty Text missing.");

        if (_button != null)
        {
            _button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (_currentData != null)
        {
            _onClickCallback?.Invoke(_currentData);
        }
    }

    public void Bind(MissionDataSO mission, int difficulty, Action<MissionDataSO> onClick)
    {
        if (mission == null) return;

        _currentData = mission;
        _onClickCallback = onClick;

        // 1. 기본 정보 (Definition)
        _missionNameText.text = mission.Definition.MissionName;

        // 2. UI 메타데이터 사용 (Logic Free)
        _locationText.text = string.IsNullOrEmpty(mission.UI.LocationName)
            ? "Unknown Area"
            : mission.UI.LocationName;

        // 3. 메타 정보
        _typeText.text = mission.Definition.Type.ToString();

        // 턴 제한 처리
        int limit = mission.Definition.TimeLimit;
        _turnLimitText.text = limit <= 0 ? "∞ Turns" : $"{limit} Turns";

        // 난이도 시각화
        ApplyDifficultyVisuals(difficulty);

        // 4. 통계 정보 (Baked Data 사용)
        _mapSizeText.text = $"{mission.UI.MapSize.x}x{mission.UI.MapSize.y}";
        _enemyCountText.text = $"Enemy: {mission.UI.EstimatedEnemyCount}";
        _squadSizeText.text = $"Squad: {mission.UI.MaxSquadSize}";
    }

    private void ApplyDifficultyVisuals(MissionDifficulty difficulty)
    {
        // TODO: 추후 DifficultyVisualDataSO 등으로 분리 권장
        Color color;
        string text;

        switch (difficulty)
        {
            case MissionDifficulty.Easy:
                text = "EASY"; color = Color.green; break;
            case MissionDifficulty.Normal:
                text = "NORMAL"; color = Color.yellow; break;
            case MissionDifficulty.Hard:
                text = "HARD"; color = new Color(1f, 0.5f, 0f); break; // Orange
            case MissionDifficulty.Hell:
                text = "HELL"; color = Color.red; break;
            default:
                text = "UNKNOWN"; color = Color.white; break;
        }

        _difficultyText.text = text;
        _difficultyText.color = color;
    }

    public void SetInteractable(bool state)
    {
        if (_button != null) _button.interactable = state;
    }
}