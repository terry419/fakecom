using UnityEngine;

[RequireComponent(typeof(UnitStatus))]
public class UnitTurnSystem : MonoBehaviour
{
    [Header("--- Turn & Action Data ---")]
    [SerializeField] private float currentTS = 0f; // Turn Score
    [field: SerializeField] public int RemainingMobility { get; private set; }

    [Header("--- Turn Flags (Visual Only) ---")]
    [SerializeField] private bool hasMoved = false;
    [SerializeField] private bool hasAttacked = false;
    [SerializeField] private int usedMoveCost = 0;

    // 계산용 캐시 데이터
    [field: SerializeField] public float LastFinalPenalty { get; private set; }
    private float damagePenaltyPool = 0f;

    // 외부 참조
    private UnitStatus _unitStatus;
    private GlobalSettingsSO _globalSettings;

    // 프로퍼티
    public float CurrentTS { get => currentTS; set => currentTS = value; }
    public bool HasMoved { get => hasMoved; set => hasMoved = value; }
    public bool HasAttacked { get => hasAttacked; set => hasAttacked = value; }
    public int UsedMoveCost { get => usedMoveCost; set => usedMoveCost = value; }

    public void Initialize(UnitStatus owner, GlobalSettingsSO settings)
    {
        _unitStatus = owner;
        _globalSettings = settings;

        // 초기 이동력 설정
        if (_unitStatus.unitData != null)
        {
            RemainingMobility = _unitStatus.unitData.Mobility;
        }

        // 초기 페널티 계산 (Start 로직 이관)
        float baseActionCost = 10f;
        int agility = _unitStatus.unitData != null ? _unitStatus.unitData.Agility : 1;
        LastFinalPenalty = (baseActionCost * 1.0f) / agility;
    }

    public void OnTurnStart()
    {
        // 상태 이상 틱 처리
        if (_unitStatus.StatusController != null)
        {
            _unitStatus.StatusController.TickEffects();
        }

        // 사망자 체크
        if (_unitStatus.IsDead)
        {
            Debug.Log($"{name}은(는) 이미 사망 상태이므로 턴을 건너뜁니다.");
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

        // 이동력 복구
        if (_unitStatus.unitData != null)
        {
            RemainingMobility = _unitStatus.unitData.Mobility;
        }
    }

    // ========================================================================
    // [Movement & Action Logic]
    // ========================================================================

    public bool CanPerformAction(int requiredMobility)
    {
        return RemainingMobility >= requiredMobility;
    }

    public void ConsumeMobility(int amount)
    {
        if (amount > RemainingMobility) RemainingMobility = 0;
        else RemainingMobility -= amount;
    }

    // ========================================================================
    // [Penalty Calculation Logic]
    // ========================================================================

    public void AddDamagePenalty(float penalty)
    {
        damagePenaltyPool += penalty;
    }

    /// <summary>
    /// 이번 턴의 행동 결과를 바탕으로 다음 턴까지의 지연 시간(TS) 페널티를 계산합니다.
    /// </summary>
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

        // 아무 행동도 하지 않았을 때의 최소 대기 시간
        if (!hasMoved && !hasAttacked) actionPenalty = 10f;

        // 피격 페널티 합산
        actionPenalty += damagePenaltyPool;
        damagePenaltyPool = 0f;

        // 상태(Condition)에 따른 보정치 적용
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

        // 최종 계산
        float finalPenalty = (actionPenalty * Random.Range(0.8f, 1.2f) * tsModifier) / agility;
        LastFinalPenalty = finalPenalty;
        return finalPenalty;
    }
}