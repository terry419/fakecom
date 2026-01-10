using System.Collections.Generic;
using UnityEngine;

public class UITimelinePanel : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("게이지 최대값 최소 기준 (이보다 작은 TS라도 이 높이 사용)")]
    [SerializeField] private float _minMaxGaugeTS = 100f;

    [Header("Spacing Settings")]
    [Tooltip("아군 깃발의 X 좌표 (음수 권장, 예: -60)")]
    [SerializeField] private float _playerXOffset = -60f;

    [Tooltip("적군 깃발의 X 좌표 (양수 권장, 예: 40)")]
    [SerializeField] private float _enemyXOffset = 40f;

    [Header("References")]
    [Tooltip("중앙 기준선 (TSGauge)")]
    [SerializeField] private RectTransform _tsGauge;

    [Header("Prefabs")]
    [SerializeField] private TimelineSlot _playerSlotPrefab;
    [SerializeField] private TimelineSlot _enemySlotPrefab;

    private Dictionary<UnitStatus, TimelineSlot> _activeSlots = new Dictionary<UnitStatus, TimelineSlot>();
    private TurnManager _cachedTurnManager;

    private void Start()
    {
        if (_tsGauge == null) return;

        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            _cachedTurnManager = turnManager;

            _cachedTurnManager.OnUnitRegistered += HandleUnitRegistered;
            _cachedTurnManager.OnUnitUnregistered += HandleUnitUnregistered;
            _cachedTurnManager.OnTick += HandleTick;

            foreach (var unit in _cachedTurnManager.AllUnits)
            {
                HandleUnitRegistered(unit);
            }

            HandleTick();
        }
    }

    private void OnDestroy()
    {
        if (_cachedTurnManager != null)
        {
            _cachedTurnManager.OnUnitRegistered -= HandleUnitRegistered;
            _cachedTurnManager.OnUnitUnregistered -= HandleUnitUnregistered;
            _cachedTurnManager.OnTick -= HandleTick;
        }
    }

    private void HandleUnitRegistered(UnitStatus unit)
    {
        if (unit == null || _activeSlots.ContainsKey(unit)) return;

        // 프리팹 결정을 위해 컨트롤러 확인
        var controller = unit.GetComponent<IUnitController>();
        TeamType team = TeamType.Neutral;

        if (controller != null) team = controller.Team;

        TimelineSlot newSlot;

        // 팀에 따른 프리팹 생성
        if (team == TeamType.Player)
            newSlot = Instantiate(_playerSlotPrefab, _tsGauge);
        else
            newSlot = Instantiate(_enemySlotPrefab, _tsGauge);

        newSlot.transform.localScale = Vector3.one;
        newSlot.gameObject.SetActive(true);

        newSlot.Bind(unit);
        _activeSlots.Add(unit, newSlot);
    }

    private void HandleUnitUnregistered(UnitStatus unit)
    {
        if (unit == null) return;
        if (_activeSlots.TryGetValue(unit, out var slot))
        {
            if (slot != null) Destroy(slot.gameObject);
            _activeSlots.Remove(unit);
        }
    }

    private void HandleTick()
    {
        UpdateAllPositions();
    }

    private void UpdateAllPositions()
    {
        if (_tsGauge == null || _activeSlots.Count == 0) return;

        float gaugeHeight = _tsGauge.rect.height;

        float currentMaxTS = _minMaxGaugeTS;
        foreach (var unit in _activeSlots.Keys)
        {
            if (unit.CurrentTS > currentMaxTS) currentMaxTS = unit.CurrentTS;
        }

        foreach (var kvp in _activeSlots)
        {
            var unit = kvp.Key;
            var slot = kvp.Value;

            if (slot == null) continue;

            // [수정 완료] _horizontalSpacing 삭제 -> _xOffsetValue 사용
            float xOffset = 0f;

            // IUnitController를 통해 팀 확인
            var controller = unit.GetComponent<IUnitController>();
            if (controller != null)
            {
                if (controller.Team == TeamType.Player)
                    xOffset = _playerXOffset; // 예: -60
                else
                    xOffset = _enemyXOffset;  // 예: 40
            }

            slot.UpdatePositionOnGauge(currentMaxTS, gaugeHeight, xOffset);
        }
    }
}