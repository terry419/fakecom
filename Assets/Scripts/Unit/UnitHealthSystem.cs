using UnityEngine;
using System;
using System.Threading.Tasks;

[RequireComponent(typeof(UnitStatus))]
public class UnitHealthSystem : MonoBehaviour
{
    [Header("--- Health Data ---")]
    [SerializeField] private int currentHP;
    public bool IsDead { get; private set; }

    public event Action<int, bool, bool, float, bool> OnDamageTaken;

    private UnitStatus _unitStatus;
    private GlobalSettingsSO _globalSettings;
    private UnitSaveData _cachedSaveData;
    private Animator _animator;

    public int CurrentHP => currentHP;

    public void Initialize(UnitStatus owner, GlobalSettingsSO settings)
    {
        _unitStatus = owner;
        _globalSettings = settings;
        _animator = GetComponent<Animator>();

        if (_unitStatus.unitData != null)
        {
            currentHP = _unitStatus.unitData.MaxHP;
        }
        IsDead = false;
    }

    public void UpdateDataFromSave(UnitSaveData saveData)
    {
        _cachedSaveData = saveData;
        currentHP = saveData.CurrentHP;
        if (currentHP <= 0) CheckSurvival(false);
    }

    public void TakeDamage(int amount, bool isMyTurn, bool isCrit, float finalPenalty, bool isStatusEffect = false)
    {
        // [LOG 1] 피격 메서드 진입 확인
        Debug.Log($"<color=red>[UnitHealthSystem] '{gameObject.name}' TakeDamage 호출됨! 데미지: {amount}</color>");

        if (IsDead)
        {
            Debug.LogWarning($"[UnitHealthSystem] '{gameObject.name}'은 이미 죽어있어서 데미지 무시됨.");
            return;
        }

        currentHP = Mathf.Max(0, currentHP - amount);

        // [LOG 2] 이벤트 구독자 확인
        if (OnDamageTaken != null)
        {
            Debug.Log($"[UnitHealthSystem] OnDamageTaken 이벤트 발송. 구독자 수: {OnDamageTaken.GetInvocationList().Length}");
            OnDamageTaken.Invoke(amount, isMyTurn, isCrit, finalPenalty, isStatusEffect);
        }
        else
        {
            Debug.LogError($"<color=red>[UnitHealthSystem] '{gameObject.name}'의 OnDamageTaken 구독자가 0명입니다! (Feedback 연결 안됨)</color>");
        }

        if (_unitStatus.unitData.UnitTeam == TeamType.Player && _cachedSaveData != null)
        {
            _cachedSaveData.CurrentHP = currentHP;
        }

        if (currentHP <= 0) CheckSurvival(isMyTurn);
    }

    public float CalculateSurvivalChance()
    {
        if (_globalSettings == null || _unitStatus.SyncSystem == null) return 0f;
        if (_unitStatus.SyncSystem.CurrentSync < 5f) return 0f;

        float pBase = 0.05f;
        float syncFactor = (_unitStatus.SyncSystem.CurrentSync - _globalSettings.minSyncOffset) / _globalSettings.syncDivisor;
        float pSync = pBase * syncFactor * _globalSettings.baseFormulaMultiplier;

        float mState = GetSurvivalMultiplier();
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

    private void CheckSurvival(bool isMyTurn)
    {
        float survivalChance = CalculateSurvivalChance();
        if (UnityEngine.Random.value <= survivalChance)
        {
            bool qteSuccess = false;
            var qteManager = ServiceLocator.Get<QTEManager>();
            if (qteManager != null) qteSuccess = qteManager.GetQTESuccessInstant(QTEType.Survival);

            if (qteSuccess)
            {
                currentHP = 1;
                if (_cachedSaveData != null) _cachedSaveData.CurrentHP = 1;
                return;
            }
        }
        HandleDeath(isMyTurn);
    }

    private async void HandleDeath(bool isMyTurn)
    {
        if (IsDead) return;
        IsDead = true;

        if (_animator != null && _unitStatus.unitData != null)
        {
            _animator.SetTrigger(_unitStatus.unitData.deathAnimationTrigger);
            await Task.Delay(100);
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