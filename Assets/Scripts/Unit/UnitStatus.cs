using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

[RequireComponent(typeof(StatusEffectController))]
public class UnitStatus : MonoBehaviour
{
    [Header("--- Data Source ---")]
    [Tooltip("기본 데이터 소스 ScriptableObject")]
    public UnitDataSO unitData;

    // 전역 설정 데이터 (GlobalSettingsSO)
    [SerializeField] private GlobalSettingsSO globalSettings;

    [Header("--- Live Data (Visual Only) ---")]
    [SerializeField] private int currentHP;
    [SerializeField] private float currentTS = 0f; // Turn Score (행동 순서 관련)
    [field: SerializeField, Tooltip("현재 남은 이동력입니다.")] public int RemainingMobility { get; private set; }
    [SerializeField] private UnitCondition condition = UnitCondition.Normal;
    
    [Header("--- Neural Sync System (New) ---")]
    [Range(0, 200)]
    [SerializeField] private float currentSync; // 신경 동기화 수치 (0~200)
    private bool hasSynchroPulseTriggered = false; // 싱크로 펄스 1회 발동 체크
    private bool isClockLocked = false;         // 시스템 과부하(클락 락) 상태 여부
    
    [Header("--- Turn Action Flags (Visual Only) ---")]
    [SerializeField] private bool hasMoved = false;
    [SerializeField] private bool hasAttacked = false;
    [SerializeField] private int usedMoveCost = 0;

    // 데미지 발생 시 다음 TS 수치에 반영하기 위한 임시 저장소
    private float damagePenaltyPool = 0f;

    // 세이브 데이터 캐싱 (참조용)
    private UnitSaveData cachedSaveData;

    // 외부 컴포넌트 및 프로퍼티
    public StatusEffectController StatusController { get; private set; }
    private Animator _animator;
    public bool IsDead { get; private set; }
    public int CurrentHP => currentHP;
    public float CurrentTS { get => currentTS; set => currentTS = value; }
    public UnitCondition Condition { get => condition; set => condition = value; }
    public bool HasMoved { get => hasMoved; set => hasMoved = value; }
    public bool HasAttacked { get => hasAttacked; set => hasAttacked = value; }
    public int UsedMoveCost { get => usedMoveCost; set => usedMoveCost = value; }

    // UnitDataSO 기반 데이터 접근 프로퍼티
    public int MaxMobility => unitData != null ? unitData.Mobility : 0;
    public int Agility => unitData != null ? unitData.Agility : 1;
    public int AttackRange => unitData != null ? unitData.Range : 1;
    public int Aim => unitData != null ? unitData.Aim : 0;
    public int Evasion => unitData != null ? unitData.Evasion : 0;
    public float CritChance => unitData != null ? unitData.CritChance : 0f;

    [field: SerializeField] public float LastFinalPenalty { get; private set; } // 마지막으로 계산된 TS 페널티 저장

    private void Awake()
    {
        StatusController = GetComponent<StatusEffectController>();
        IsDead = false;

        // [배치] UnitDataSO 할당 체크
        if (unitData == null)
        {
            Debug.LogError($"[UnitStatus] {gameObject.name}의 UnitDataSO가 설정되지 않았습니다!");
            return;
        }

        // 기본 상태 초기화 (SceneInitializer 등에서 덮어쓰기 가능)
        currentHP = unitData.MaxHP;
        RemainingMobility = unitData.Mobility;

        // Neural Sync 초기화
        // unitData에 기본 수치가 있다면 사용, 없다면 100으로 초기화
        currentSync = 100f;
    }

    private void Start()
    {
        float baseActionCost = 10f;
        LastFinalPenalty = (baseActionCost * 1.0f) / Agility;
    }
    
    /// <summary>
    /// 세이브 데이터(UnitSaveData)로부터 유닛 정보를 초기화하고 캐싱합니다.
    /// SceneInitializer 또는 로딩 로직에서 호출됩니다.
    /// </summary>
    public void InitializeFromSaveData(UnitSaveData saveData)
    {
        if (saveData == null) return;

        // 세이브 데이터 캐싱
        cachedSaveData = saveData;

        // 현재 체력 복구
        currentHP = saveData.CurrentHP;

        // (확장용) 경험치 등의 추가 데이터 로드 시 여기서 처리
        // this.experience = saveData.Experience;

        Debug.Log($"[{gameObject.name}] 세이브 데이터 초기화 완료 (HP: {currentHP})");

        if (currentHP <= 0) CheckSurvival(false);
    }
    
    public bool CanPerformAction(int requiredMobility)
    {
        return RemainingMobility >= requiredMobility;
    }

    /// <summary>
    /// 이번 턴의 행동 결과를 바탕으로 다음 턴까지의 지연 시간(TS) 페널티를 계산합니다.
    /// </summary>
    public float CalculateNextTurnPenalty()
    {
        float actionPenalty = 0f;
        float tsModifier = 1.0f;

        if (hasAttacked) actionPenalty += 60f; // 공격 페널티
        if (hasMoved)
        {
            // 이동 거리에 따른 피로도 기반 페널티 계산
            float fatigueRate = (float)usedMoveCost / MaxMobility;
            actionPenalty += (40f * fatigueRate);
        }

        // 아무 행동도 하지 않았을 때의 최소 대기 시간
        if (!hasMoved && !hasAttacked) actionPenalty = 10f;

        // 피격 시 쌓인 페널티 추가 후 초기화
        actionPenalty += damagePenaltyPool;
        damagePenaltyPool = 0f;

        // 상태(Condition)에 따른 보정치 적용
        switch (condition)
        {
            case UnitCondition.Hopeful:
                tsModifier = 0.8f;  // 20% 단축 (이득)
                break;
            case UnitCondition.Inspired:
                tsModifier = 0.9f;  // 10% 단축
                break;
            case UnitCondition.Normal:
            case UnitCondition.Incapacitated:
                tsModifier = 1.0f;  // 표준
                break;
            case UnitCondition.Fleeing:
                tsModifier = 1.5f;  // 50% 지연 (불이익)
                break;
            case UnitCondition.FriendlyFire:
            case UnitCondition.SelfHarm:
                tsModifier = 2.0f;  // 2배 지연 (매우 불리)
                break;
        }

        // 최종 계산: (행동 페널티 * 난수 보정 * 상태 보정) / 민첩성
        float finalPenalty = (actionPenalty * Random.Range(0.8f, 1.2f) * tsModifier) / Agility;
        LastFinalPenalty = finalPenalty;
        return finalPenalty;
    }
    
    // ========================================================================
    // [시스템] Neural Sync Logic (동기화 관련 로직)
    // ========================================================================
    
    /// <summary>
    /// 신경 동기화 수치를 갱신하고 상태 변화를 체크합니다.
    /// </summary>
    public void UpdateSync(float amount, string reason = "")
    {
        if (globalSettings == null) return;

        // 1. 변화 전 수치 기록
        float previousSync = currentSync;

        // 2. 수치 합산 및 클램핑 (0~200)
        currentSync = Mathf.Clamp(currentSync + amount, 0f, 200f);

        // 3. [ClockLock 해제 검사]
        // 시스템 잠금 상태에서 수치가 회복되어 기준치(100)를 넘었는지 확인
        if (isClockLocked && amount > 0)
        {
            if (currentSync >= 100f)
            {
                isClockLocked = false;
                Debug.Log($"<color=green>[System Restored] {name}의 ClockLock이 해제되었습니다!</color>");
            }
        }

        // 4. [Synchro-Pulse 발동 검사]
        // 정상 범주(50 이상)에서 미만으로 떨어졌을 때 1회에 한해 펄스 발생
        bool isDroppingBelowThreshold = (previousSync >= globalSettings.thresholdNormal && currentSync < globalSettings.thresholdNormal);

        if (!isClockLocked && isDroppingBelowThreshold && !hasSynchroPulseTriggered)
        {
            HandleSynchroPulse();
        }

        // 5. 상태 갱신
        UpdateConditionFromSync();
    }


    /// <summary>
    /// 싱크로 수치가 급격히 낮아질 때 발생하는 최후의 보루(펄스)를 처리합니다.
    /// </summary>
    private void HandleSynchroPulse()
    {
        if (hasSynchroPulseTriggered) return;

        hasSynchroPulseTriggered = true; // 일회성 발동 체크

        // 1. 오버클럭 시도 확률 체크 (예: 5%)
        if (Random.value <= globalSettings.baseOverclockChance)
        {
            Debug.Log($"<color=cyan>[Synchro-Pulse] {name} 임계 동기화 도달! QTE 발생...</color>");

            // 2. QTE 매니저 호출
            var qte = ServiceLocator.Get<QTEManager>();
            if (qte != null)
            {
                qte.StartQTE(QTEType.SynchroPulse, (bool isSuccess) =>
                {
                    // 3. QTE 결과 반영
                    if (isSuccess)
                    {
                        // [성공] 오버클럭 발동
                        currentSync = globalSettings.overclockSuccessValue; // 약 160
                        isClockLocked = false;
                        UpdateConditionFromSync();
                        Debug.Log($"<color=cyan>[Overclock] {name} 시스템 한계 돌파!</color>");
                    }
                    else
                    {
                        // [실패] 시스템 잠금
                        isClockLocked = true;
                        UpdateConditionFromSync();
                        Debug.Log("<color=red>[ClockLock] QTE 실패... 시스템이 잠깁니다.</color>");
                    }
                });
            }
            else
            {
                // 매니저가 없을 경우 기본적으로 잠금 처리
                isClockLocked = true;
                UpdateConditionFromSync();
            }
        }
        else
        {
            // 확률을 뚫지 못했을 경우 (95% 등) 바로 잠금
            isClockLocked = true;
            UpdateConditionFromSync();
            Debug.Log("<color=red>[ClockLock] 오버클럭 기회를 놓쳤습니다. (확률 실패)</color>");
        }
    }

    /// <summary>
    /// 현재 신경 동기화 수치에 기반하여 유닛의 상태(Condition)를 결정합니다.
    /// </summary>
    private void UpdateConditionFromSync()
    {
        // 1. [ClockLock 우선 체크]
        // 시스템 잠금 상태라면 수치와 무관하게 '불능' 상태로 처리
        if (isClockLocked)
        {
            condition = UnitCondition.Incapacitated;
            return;
        }

        // 2. [수치 기반 상태 결정]
        UnitCondition newCondition;

        if (currentSync >= globalSettings.thresholdHopeful) newCondition = UnitCondition.Hopeful;
        else if (currentSync >= globalSettings.thresholdInspired) newCondition = UnitCondition.Inspired;
        else if (currentSync >= globalSettings.thresholdNormal) newCondition = UnitCondition.Normal;
        else
        {
            // 50 미만 하향 구간
            if (currentSync >= globalSettings.thresholdIncapacitated) newCondition = UnitCondition.Incapacitated;
            else if (currentSync >= globalSettings.thresholdFleeing) newCondition = UnitCondition.Fleeing;
            else if (currentSync >= globalSettings.thresholdFriendlyFire) newCondition = UnitCondition.FriendlyFire;
            else newCondition = UnitCondition.SelfHarm;
        }

        if (condition != newCondition)
        {
            condition = newCondition;
        }
    }
    
    // ========================================================================
    // [전투] TakeDamage & Survival Logic
    // ========================================================================

    public void TakeDamage(int amount, bool isMyTurn, bool isCrit, float finalPenalty, bool isStatusEffect = false)
    {
        if (IsDead) return;
        currentHP = Mathf.Max(0, currentHP - amount);

        // 1. 사망 체크 시 isMyTurn 전달!
        if (currentHP <= 0)
        {
            //CheckSurvival(isMyTurn);
            return;
        }

        //if (unitData.UnitTeam == UnitDataSO.Team.Player && cachedSaveData != null)
        //{
        //    cachedSaveData.CurrentHP = currentHP;
        //}

        float lastActionCost = finalPenalty;
        var settings = ServiceLocator.Get<GlobalSettingsSO>();
        float ratioNormal = settings != null ? settings.tsPenaltyRatioNormal : 0.1f;
        float ratioCrit = settings != null ? settings.tsPenaltyRatioCrit : 0.2f;

        float penaltyVal = isCrit ? lastActionCost * ratioCrit : lastActionCost * ratioNormal;

        if (!isStatusEffect)
        {
            //UpdateSync(isCrit ? -5f : 0f, "TakeDamage");

            if (!isMyTurn)
            {
                float oldTS = currentTS;
                currentTS += penaltyVal;
                var turnManager = ServiceLocator.Get<TurnManager>();
                turnManager?.UpdateUnitTurnDelay(this, oldTS, currentTS, isCrit);
            }
            else
            {
                damagePenaltyPool += penaltyVal;
            }

            Debug.Log($"[{gameObject.name}] 피격! HP:{currentHP}, TS페널티:{penaltyVal}");
        }
    }
    
    /// <summary>
    /// 유닛의 사망 직전 생존 확률을 계산합니다.
    /// </summary>
    public float CalculateSurvivalChance()
    {
        if (globalSettings == null) return 0f;
        if (currentSync < 5f) return 0f; // 최소 동기화 요구치

        // 1. 기본 생존 확률 (유닛 데이터 기반)
        float pBase = 0.05f;

        // 동기화 수치가 높을수록 생존 확률 증가
        float syncFactor = (currentSync - globalSettings.minSyncOffset) / globalSettings.syncDivisor;
        float pSync = pBase * syncFactor * globalSettings.baseFormulaMultiplier;

        // 2. 유닛 상태에 따른 가중치
        float mState = GetSurvivalMultiplier();

        // 3. 디버프에 의한 생존율 페널티
        float mDebuff = 1.0f;
        if (StatusController != null)
        {
            mDebuff = StatusController.GetTotalSurvivalPenalty();
        }

        return Mathf.Clamp01(pSync * mState * mDebuff);
    }

    private float GetSurvivalMultiplier()
    {
        if (globalSettings == null) return 1.0f;

        switch (condition)
        {
            case UnitCondition.Hopeful: return globalSettings.multiplierHopeful;
            case UnitCondition.Inspired: return globalSettings.multiplierInspired;
            case UnitCondition.Normal: return globalSettings.multiplierNormal;
            case UnitCondition.Incapacitated: return globalSettings.multiplierIncapacitated;
            case UnitCondition.Fleeing: return globalSettings.multiplierFleeing;
            case UnitCondition.FriendlyFire: return globalSettings.multiplierFriendlyFire;
            case UnitCondition.SelfHarm: return globalSettings.multiplierSelfHarm;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// 체력이 0이 되었을 때 생존 여부를 체크합니다 (확률 및 QTE).
    /// </summary>
    private void CheckSurvival(bool isMyTurn)
    {
        // 1단계: 생존 확률 계산 (신경 동기화 기반)
        float survivalChance = CalculateSurvivalChance();

        // 2단계: 주사위 굴리기
        if (UnityEngine.Random.value <= survivalChance)
        {
            bool qteSuccess = false;
            var qteManager = ServiceLocator.Get<QTEManager>();

            if (qteManager != null)
            {
                // 확률을 통과한 유닛에게 주어지는 마지막 기회 (QTE)
                qteSuccess = qteManager.GetQTESuccessInstant(QTEType.Survival);
            }

            // QTE 성공 시 체력 1로 생존
            if (qteSuccess)
            {
                currentHP = 1;
                if (cachedSaveData != null) cachedSaveData.CurrentHP = 1;

                Debug.Log($"[Survival] {name} 기사회생! (확률:{survivalChance:P1}, QTE:{qteSuccess})");
                return;
            }
        }

        // 확률 실패 혹은 QTE 실패 시 최종 사망
        HandleDeath(isMyTurn);
    }
    
    public void ConsumeMobility(int amount)
    {
        if (amount > RemainingMobility) RemainingMobility = 0;
        else RemainingMobility -= amount;
    }

    private async void HandleDeath(bool isMyTurn)
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"{gameObject.name} 사망하였습니다.");

        // 애니메이션 대기 로직 적용
        if (_animator != null && unitData != null)
        {
            _animator.SetTrigger(unitData.deathAnimationTrigger);

            // 상태 전환 대기 (0.1초)
            await Task.Delay(100);

            // 애니메이션 종료 대기
            while (_animator.GetCurrentAnimatorStateInfo(0).IsName(unitData.deathStateName) &&
                   _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                await Task.Yield();
            }
        }
        else
        {
            await Task.Delay(500); // 애니메이터 없으면 그냥 0.5초 대기
        }

        gameObject.SetActive(false);

        // 내 턴일 경우 턴 넘기기
        if (isMyTurn)
        {
            var turnManager = ServiceLocator.Get<TurnManager>();
            turnManager?.EndTurn();
        }
    }

    public void OnTurnStart()
    {
        // 턴 시작 시 상태 이상 지속시간/효과 갱신
        StatusController.TickEffects();

        if (IsDead)
        {
            Debug.Log($"{gameObject.name}은(는) 이미 사망 상태이므로 턴을 건너뜁니다.");

            // 사망한 유닛의 턴이 돌아온 경우 즉시 턴 종료 알림
            var turnManager = ServiceLocator.Get<TurnManager>();
            if (turnManager != null)
            {
                turnManager.EndTurn();
            }
            return;
        }
    }

    public void ResetTurnData()
    {
        hasMoved = false;
        hasAttacked = false;
        usedMoveCost = 0;
        RemainingMobility = MaxMobility;
    }
}
