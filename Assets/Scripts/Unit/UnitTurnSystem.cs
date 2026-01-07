using UnityEngine;

[RequireComponent(typeof(UnitStatus))]
public class UnitTurnSystem : MonoBehaviour
{
    [Header("--- Turn & Action Data ---")]
    [SerializeField] private float currentTS = 0f;

    [field: SerializeField] public int RemainingMobility { get; private set; }

    [Header("--- Turn Flags (Visual Only) ---")]
    [SerializeField] private bool hasMoved = false;
    [SerializeField] private bool hasAttacked = false;
    [SerializeField] private int usedMoveCost = 0;

    [field: SerializeField] public float LastFinalPenalty { get; private set; }
    private float damagePenaltyPool = 0f;

    private UnitStatus _unitStatus;
    private GlobalSettingsSO _globalSettings;

    public float CurrentTS { get => currentTS; set => currentTS = value; }

    // 읽기 전용 프로퍼티 (수정은 메서드로)
    public bool HasMoved => hasMoved;
    public bool HasAttacked => hasAttacked;
    public int UsedMoveCost => usedMoveCost;

    public void Initialize(UnitStatus owner, GlobalSettingsSO settings)
    {
        _unitStatus = owner;
        _globalSettings = settings;

        if (_unitStatus.unitData != null)
            RemainingMobility = _unitStatus.unitData.Mobility;

        float baseActionCost = 10f;
        int agility = _unitStatus.unitData != null ? _unitStatus.unitData.Agility : 1;
        LastFinalPenalty = (baseActionCost * 1.0f) / agility;
    }

    public void OnTurnStart()
    {
        if (_unitStatus.StatusController != null)
            _unitStatus.StatusController.TickEffects();

        if (_unitStatus.IsDead)
        {
            var turnManager = ServiceLocator.Get<TurnManager>();
            turnManager?.EndTurn();
            return;
        }
    }

    public void ResetTurnData()
    {
        hasMoved = false;
        hasAttacked = false;
        usedMoveCost = 0;

        if (_unitStatus.unitData != null)
            RemainingMobility = _unitStatus.unitData.Mobility;
    }

    // ========================================================================
    // [Action Logic] 
    // ========================================================================

    public bool CanPerformAction(int requiredMobility)
    {
        return RemainingMobility >= requiredMobility;
    }

    // [Fix] 메서드 이름 'ConsumeMobility'로 통일 (UnitStatus와 일치)
    public void ConsumeMobility(int amount)
    {
        if (amount > RemainingMobility) RemainingMobility = 0;
        else RemainingMobility -= amount;
        usedMoveCost += amount;
    }

    // [Fix] 외부 Setter 메서드 제공
    public void SetHasAttacked(bool state) => hasAttacked = state;
    public void SetHasMoved(bool state) => hasMoved = state;
    public void SetUsedMoveCost(int cost) => usedMoveCost = cost;

    // ========================================================================
    // [Penalty Logic]
    // ========================================================================

    public void AddDamagePenalty(float penalty)
    {
        damagePenaltyPool += penalty;
    }

    public float CalculateNextTurnPenalty()
    {
        float actionPenalty = 0f;
        float tsModifier = 1.0f;
        int maxMobility = _unitStatus.unitData != null ? _unitStatus.unitData.Mobility : 10;
        int agility = _unitStatus.unitData != null ? _unitStatus.unitData.Agility : 1;

        if (hasAttacked) actionPenalty += 60f;
        if (hasMoved)
        {
            float fatigueRate = (float)usedMoveCost / maxMobility;
            actionPenalty += (40f * fatigueRate);
        }

        if (!hasMoved && !hasAttacked) actionPenalty = 10f;

        actionPenalty += damagePenaltyPool;
        damagePenaltyPool = 0f;

        switch (_unitStatus.Condition)
        {
            case UnitCondition.Hopeful: tsModifier = 0.8f; break;
            case UnitCondition.Inspired: tsModifier = 0.9f; break;
            case UnitCondition.Normal:
            case UnitCondition.Incapacitated: tsModifier = 1.0f; break;
            case UnitCondition.Fleeing: tsModifier = 1.5f; break;
            case UnitCondition.FriendlyFire:
            case UnitCondition.SelfHarm: tsModifier = 2.0f; break;
        }

        float finalPenalty = (actionPenalty * Random.Range(0.8f, 1.2f) * tsModifier) / agility;
        LastFinalPenalty = finalPenalty;
        return finalPenalty;
    }
}