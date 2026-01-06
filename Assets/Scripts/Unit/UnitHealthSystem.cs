using UnityEngine;
using System.Threading.Tasks;

[RequireComponent(typeof(UnitStatus))]
public class UnitHealthSystem : MonoBehaviour
{
    [Header("--- Health Data ---")]
    [SerializeField] private int currentHP;
    public bool IsDead { get; private set; }

    // 외부 참조
    private UnitStatus _unitStatus;
    private GlobalSettingsSO _globalSettings;
    private UnitSaveData _cachedSaveData;
    private Animator _animator;

    // 프로퍼티
    public int CurrentHP => currentHP;

    /// <summary>
    /// 초기화: UnitStatus로부터 기본 정보와 설정을 받아옵니다.
    /// </summary>
    public void Initialize(UnitStatus owner, GlobalSettingsSO settings)
    {
        _unitStatus = owner;
        _globalSettings = settings;
        _animator = GetComponent<Animator>();

        // 기본 체력 설정 (데이터가 있다면)
        if (_unitStatus.unitData != null)
        {
            currentHP = _unitStatus.unitData.MaxHP;
        }

        IsDead = false;
    }

    /// <summary>
    /// 세이브 데이터 로드 시 체력 정보를 갱신합니다.
    /// </summary>
    public void UpdateDataFromSave(UnitSaveData saveData)
    {
        _cachedSaveData = saveData;
        currentHP = saveData.CurrentHP;

        if (currentHP <= 0)
        {
            CheckSurvival(false);
        }
    }

    // ========================================================================
    // [전투] TakeDamage & Survival Logic
    // ========================================================================

    public void TakeDamage(int amount, bool isMyTurn, bool isCrit, float finalPenalty, bool isStatusEffect = false)
    {
        if (IsDead) return;

        currentHP = Mathf.Max(0, currentHP - amount);

        // 1. 사망 체크
        if (currentHP <= 0)
        {
            CheckSurvival(isMyTurn);
            return;
        }

        // 2. 세이브 데이터 갱신 (플레이어 팀인 경우)
        if (_unitStatus.unitData.UnitTeam == TeamType.Player && _cachedSaveData != null)
        {
            _cachedSaveData.CurrentHP = currentHP;
        }

        // 3. TS 페널티 계산 로직
        float lastActionCost = finalPenalty;
        float ratioNormal = _globalSettings != null ? _globalSettings.tsPenaltyRatioNormal : 0.1f;
        float ratioCrit = _globalSettings != null ? _globalSettings.tsPenaltyRatioCrit : 0.2f;

        float penaltyVal = isCrit ? lastActionCost * ratioCrit : lastActionCost * ratioNormal;

        if (!isStatusEffect)
        {
            // Sync 시스템에 피격 알림
            if (_unitStatus.SyncSystem != null)
            {
                _unitStatus.SyncSystem.UpdateSync(isCrit ? -5f : 0f, "TakeDamage");
            }

            if (!isMyTurn)
            {
                // 비턴 피격: 턴 순서 밀림
                float oldTS = _unitStatus.CurrentTS;
                _unitStatus.CurrentTS += penaltyVal;

                var turnManager = ServiceLocator.Get<TurnManager>();
                turnManager?.UpdateUnitTurnDelay(_unitStatus, oldTS, _unitStatus.CurrentTS, isCrit);
            }
            else
            {
                // 내 턴 피격(반격 등): 다음 행동 페널티 누적
                // (주의: damagePenaltyPool은 아직 UnitStatus에 존재하므로 메서드로 접근)
                _unitStatus.AddDamagePenalty(penaltyVal);
            }

            Debug.Log($"[{gameObject.name}] 피격! HP:{currentHP}, TS페널티:{penaltyVal}");
        }
    }

    /// <summary>
    /// 유닛의 사망 직전 생존 확률을 계산합니다.
    /// </summary>
    public float CalculateSurvivalChance()
    {
        if (_globalSettings == null || _unitStatus.SyncSystem == null) return 0f;

        // 최소 동기화 요구치 체크
        if (_unitStatus.SyncSystem.CurrentSync < 5f) return 0f;

        // 1. 기본 확률
        float pBase = 0.05f;

        // 2. 동기화 수치 보정
        float syncFactor = (_unitStatus.SyncSystem.CurrentSync - _globalSettings.minSyncOffset) / _globalSettings.syncDivisor;
        float pSync = pBase * syncFactor * _globalSettings.baseFormulaMultiplier;

        // 3. 상태(Condition) 보정
        float mState = GetSurvivalMultiplier();

        // 4. 상태이상(StatusEffect) 보정
        float mDebuff = 1.0f;
        if (_unitStatus.StatusController != null)
        {
            mDebuff = _unitStatus.StatusController.GetTotalSurvivalPenalty();
        }

        return Mathf.Clamp01(pSync * mState * mDebuff);
    }

    private float GetSurvivalMultiplier()
    {
        if (_globalSettings == null) return 1.0f;

        switch (_unitStatus.Condition)
        {
            case UnitCondition.Hopeful: return _globalSettings.multiplierHopeful;
            case UnitCondition.Inspired: return _globalSettings.multiplierInspired;
            case UnitCondition.Normal: return _globalSettings.multiplierNormal;
            case UnitCondition.Incapacitated: return _globalSettings.multiplierIncapacitated;
            case UnitCondition.Fleeing: return _globalSettings.multiplierFleeing;
            case UnitCondition.FriendlyFire: return _globalSettings.multiplierFriendlyFire;
            case UnitCondition.SelfHarm: return _globalSettings.multiplierSelfHarm;
            default: return 1.0f;
        }
    }

    /// <summary>
    /// 체력이 0이 되었을 때 생존 여부를 체크합니다 (확률 및 QTE).
    /// </summary>
    private void CheckSurvival(bool isMyTurn)
    {
        float survivalChance = CalculateSurvivalChance();

        // 주사위 굴리기
        if (UnityEngine.Random.value <= survivalChance)
        {
            bool qteSuccess = false;
            var qteManager = ServiceLocator.Get<QTEManager>();

            if (qteManager != null)
            {
                // 마지막 기회 (QTE)
                qteSuccess = qteManager.GetQTESuccessInstant(QTEType.Survival);
            }

            if (qteSuccess)
            {
                currentHP = 1;
                if (_cachedSaveData != null) _cachedSaveData.CurrentHP = 1;

                Debug.Log($"[Survival] {name} 기사회생! (확률:{survivalChance:P1}, QTE:{qteSuccess})");
                return;
            }
        }

        // 최종 사망
        HandleDeath(isMyTurn);
    }

    private async void HandleDeath(bool isMyTurn)
    {
        if (IsDead) return;
        IsDead = true;
        Debug.Log($"{gameObject.name} 사망하였습니다.");

        if (_animator != null && _unitStatus.unitData != null)
        {
            _animator.SetTrigger(_unitStatus.unitData.deathAnimationTrigger);

            await Task.Delay(100);

            // 애니메이션 종료 대기
            while (_animator.GetCurrentAnimatorStateInfo(0).IsName(_unitStatus.unitData.deathStateName) &&
                   _animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                await Task.Yield();
            }
        }
        else
        {
            await Task.Delay(500);
        }

        gameObject.SetActive(false);

        if (isMyTurn)
        {
            var turnManager = ServiceLocator.Get<TurnManager>();
            turnManager?.EndTurn();
        }
    }
}