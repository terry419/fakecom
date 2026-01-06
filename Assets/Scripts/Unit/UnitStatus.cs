using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(UnitSyncSystem))]
[RequireComponent(typeof(UnitHealthSystem))]
[RequireComponent(typeof(UnitTurnSystem))]
public class UnitStatus : MonoBehaviour
{
    [Header("--- Data Source ---")]
    [Tooltip("기본 데이터 소스 ScriptableObject")]
    public UnitDataSO unitData;

    [SerializeField] private GlobalSettingsSO globalSettings;

    // 세이브 데이터 캐싱
    private UnitSaveData cachedSaveData;

    // 외부 컴포넌트 접근자
    public StatusEffectController StatusController { get; private set; }
    public UnitSyncSystem SyncSystem { get; private set; }
    public UnitHealthSystem HealthSystem { get; private set; }
    public UnitTurnSystem TurnSystem { get; private set; }

    // ========================================================================
    // [Proxy Properties] 하위 시스템 데이터 연결
    // ========================================================================
    public bool IsDead => HealthSystem != null && HealthSystem.IsDead;
    public int CurrentHP => HealthSystem != null ? HealthSystem.CurrentHP : 0;

    // Condition은 SyncSystem이 관리하지만 UnitStatus에 필드를 둠 (중앙 공유)
    public UnitCondition Condition { get => condition; set => condition = value; }
    [SerializeField] private UnitCondition condition = UnitCondition.Normal;

    // TurnSystem 데이터 연결
    public float CurrentTS
    {
        get => TurnSystem != null ? TurnSystem.CurrentTS : 0f;
        set { if (TurnSystem != null) TurnSystem.CurrentTS = value; }
    }
    public int RemainingMobility => TurnSystem != null ? TurnSystem.RemainingMobility : 0;
    public bool HasMoved
    {
        get => TurnSystem != null ? TurnSystem.HasMoved : false;
        set { if (TurnSystem != null) TurnSystem.HasMoved = value; }
    }
    public bool HasAttacked
    {
        get => TurnSystem != null ? TurnSystem.HasAttacked : false;
        set { if (TurnSystem != null) TurnSystem.HasAttacked = value; }
    }
    public int UsedMoveCost
    {
        get => TurnSystem != null ? TurnSystem.UsedMoveCost : 0;
        set { if (TurnSystem != null) TurnSystem.UsedMoveCost = value; }
    }
    public float LastFinalPenalty => TurnSystem != null ? TurnSystem.LastFinalPenalty : 0f;

    // UnitDataSO 숏컷
    public int MaxMobility => unitData != null ? unitData.Mobility : 0;
    public int Agility => unitData != null ? unitData.Agility : 1;
    public int AttackRange => unitData != null ? unitData.Range : 1;
    public int Aim => unitData != null ? unitData.Aim : 0;
    public int Evasion => unitData != null ? unitData.Evasion : 0;
    public float CritChance => unitData != null ? unitData.CritChance : 0f;

    private void Awake()
    {
        StatusController = GetComponent<StatusEffectController>();
        SyncSystem = GetComponent<UnitSyncSystem>();
        HealthSystem = GetComponent<UnitHealthSystem>();
        TurnSystem = GetComponent<UnitTurnSystem>();

        if (unitData == null)
        {
            Debug.LogError($"[UnitStatus] {gameObject.name}의 UnitDataSO가 설정되지 않았습니다!");
            return;
        }

        if (SyncSystem != null) SyncSystem.Initialize(this, globalSettings);
        if (HealthSystem != null) HealthSystem.Initialize(this, globalSettings);
        if (TurnSystem != null) TurnSystem.Initialize(this, globalSettings);
    }

    public void InitializeFromSaveData(UnitSaveData saveData)
    {
        if (saveData == null) return;
        cachedSaveData = saveData;
        if (HealthSystem != null) HealthSystem.UpdateDataFromSave(saveData);
    }

    // ========================================================================
    // [연결 메서드 (Proxy Methods)] - 에러 해결의 핵심
    // 외부(TurnManager 등)에서는 여전히 UnitStatus의 메서드를 호출하므로
    // 여기서 받아서 하위 시스템으로 토스해줍니다.
    // ========================================================================

    // 1. TurnManager가 찾는 메서드들 -> TurnSystem으로 연결
    public void ResetTurnData()
    {
        TurnSystem?.ResetTurnData();
    }

    public void OnTurnStart()
    {
        TurnSystem?.OnTurnStart();
    }

    public float CalculateNextTurnPenalty()
    {
        return TurnSystem != null ? TurnSystem.CalculateNextTurnPenalty() : 0f;
    }

    // 2. UnitHealthSystem이 찾는 메서드 -> TurnSystem으로 연결
    public void AddDamagePenalty(float penalty)
    {
        TurnSystem?.AddDamagePenalty(penalty);
    }

    // 3. 기타 필요한 연결
    public bool CanPerformAction(int requiredMobility)
    {
        return TurnSystem != null && TurnSystem.CanPerformAction(requiredMobility);
    }

    public void ConsumeMobility(int amount)
    {
        TurnSystem?.ConsumeMobility(amount);
    }

    // 4. 전투 관련 연결 (혹시 외부에서 직접 호출할 경우를 대비)
    public void TakeDamage(int amount, bool isMyTurn, bool isCrit, float finalPenalty, bool isStatusEffect = false)
    {
        HealthSystem?.TakeDamage(amount, isMyTurn, isCrit, finalPenalty, isStatusEffect);
    }
}