using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(StatusEffectController))]
[RequireComponent(typeof(UnitSyncSystem))]
[RequireComponent(typeof(UnitHealthSystem))]
[RequireComponent(typeof(UnitTurnSystem))]
public class UnitStatus : MonoBehaviour
{
    [Header("--- Data Source ---")]
    public UnitDataSO unitData;
    [SerializeField] private GlobalSettingsSO globalSettings;

    private UnitSaveData cachedSaveData;

    public StatusEffectController StatusController { get; private set; }
    public UnitSyncSystem SyncSystem { get; private set; }
    public UnitHealthSystem HealthSystem { get; private set; }
    public UnitTurnSystem TurnSystem { get; private set; }

    // Proxy Properties
    public bool IsDead => HealthSystem != null && HealthSystem.IsDead;
    public int CurrentHP => HealthSystem != null ? HealthSystem.CurrentHP : 0;

    public UnitCondition Condition { get => condition; set => condition = value; }
    [SerializeField] private UnitCondition condition = UnitCondition.Normal;

    private void Start()
    {
        // TurnManager 찾아서 나를 등록해줘!
        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            turnManager.RegisterUnit(this);
        }
        else
        {
            // 만약 못 찾으면 0.5초 뒤 재시도하는 코루틴 등을 쓸 수도 있지만,
            // 보통은 TurnManager가 먼저 초기화되므로 바로 될 것입니다.
            Debug.LogWarning($"[UnitStatus] Failed to register {name} to TurnManager.");
        }
    }

    private void OnDestroy()
    {
        // 죽거나 파괴될 때 등록 해제
        if (ServiceLocator.TryGet<TurnManager>(out var turnManager))
        {
            turnManager.UnregisterUnit(this);
        }
    }

    public float CurrentTS
    {
        get => TurnSystem != null ? TurnSystem.CurrentTS : 0f;
        set { if (TurnSystem != null) TurnSystem.CurrentTS = value; }
    }
    public int RemainingMobility => TurnSystem != null ? TurnSystem.RemainingMobility : 0;

    public bool HasMoved
    {
        get => TurnSystem != null ? TurnSystem.HasMoved : false;
        set { if (TurnSystem != null) TurnSystem.SetHasMoved(value); }
    }
    public bool HasAttacked
    {
        get => TurnSystem != null ? TurnSystem.HasAttacked : false;
        set { if (TurnSystem != null) TurnSystem.SetHasAttacked(value); }
    }
    public int UsedMoveCost
    {
        get => TurnSystem != null ? TurnSystem.UsedMoveCost : 0;
        set { if (TurnSystem != null) TurnSystem.SetUsedMoveCost(value); }
    }
    public float LastFinalPenalty => TurnSystem != null ? TurnSystem.LastFinalPenalty : 0f;

    // UnitDataSO Shortcuts
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
            Debug.LogError($"[UnitStatus] {gameObject.name}의 UnitDataSO가 설정되지 않았습니다!");

        if (SyncSystem != null) SyncSystem.Initialize(this, globalSettings);
        if (HealthSystem != null) HealthSystem.Initialize(this, globalSettings);
        if (TurnSystem != null) TurnSystem.Initialize(this, globalSettings);
    }

    // [추가] 이벤트 구독/해제
    private void OnEnable()
    {
        if (HealthSystem != null)
        {
            HealthSystem.OnDamageTaken += HandleDamageTaken;
        }
    }

    private void OnDisable()
    {
        if (HealthSystem != null)
        {
            HealthSystem.OnDamageTaken -= HandleDamageTaken;
        }
    }

    public void InitializeFromSaveData(UnitSaveData saveData)
    {
        if (saveData == null) return;
        cachedSaveData = saveData;
        if (HealthSystem != null) HealthSystem.UpdateDataFromSave(saveData);
    }

    // [추가] 피격 시 로직 처리 (기존 HealthSystem에서 이동됨)
    private void HandleDamageTaken(int damage, bool isMyTurn, bool isCrit, float basePenalty, bool isStatusEffect)
    {
        if (isStatusEffect) return; // 상태이상 데미지는 TS 페널티 없음

        // 1. Sync 감소
        if (SyncSystem != null)
        {
            SyncSystem.UpdateSync(isCrit ? -5f : 0f, "TakeDamage");
        }

        // 2. TS 페널티 계산
        float ratioNormal = globalSettings != null ? globalSettings.tsPenaltyRatioNormal : 0.1f;
        float ratioCrit = globalSettings != null ? globalSettings.tsPenaltyRatioCrit : 0.2f;
        float penaltyVal = isCrit ? basePenalty * ratioCrit : basePenalty * ratioNormal;

        // 3. 턴 밀림 처리
        if (!isMyTurn)
        {
            float oldTS = CurrentTS;
            CurrentTS += penaltyVal;

            var turnManager = ServiceLocator.Get<TurnManager>();
            turnManager?.UpdateUnitTurnDelay(this, oldTS, CurrentTS, isCrit);
        }
        else
        {
            // 내 턴이면 다음 행동 페널티에 누적
            TurnSystem?.AddDamagePenalty(penaltyVal);
        }
    }

    // Proxy Methods
    public void ResetTurnData() => TurnSystem?.ResetTurnData();
    public void OnTurnStart() => TurnSystem?.OnTurnStart();
    public float CalculateNextTurnPenalty() => TurnSystem != null ? TurnSystem.CalculateNextTurnPenalty() : 0f;
    public void AddDamagePenalty(float penalty) => TurnSystem?.AddDamagePenalty(penalty);
    public void AddTurnPenalty(float penalty) => TurnSystem?.AddDamagePenalty(penalty);
    public bool CanPerformAction(int requiredMobility) => TurnSystem != null && TurnSystem.CanPerformAction(requiredMobility);
    public void ConsumeMobility(int amount) => TurnSystem?.ConsumeMobility(amount);

    // [변경] 단순 중계 (로직은 HandleDamageTaken 이벤트 리스너가 처리함)
    public void TakeDamage(int amount, bool isMyTurn, bool isCrit, float finalPenalty, bool isStatusEffect = false)
    {
        HealthSystem?.TakeDamage(amount, isMyTurn, isCrit, finalPenalty, isStatusEffect);
    }
}