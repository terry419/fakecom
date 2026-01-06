using UnityEngine;
using Cysharp.Threading.Tasks;

// [Action Plan 반영] bool 대신 명확한 결과를 반환하는 Enum
public enum AttackResult
{
    Miss,       // 빗나감
    Hit,        // 일반 명중
    Critical    // 치명타
}

public class CombatManager : MonoBehaviour, IInitializable
{
    [Header("Combat Balance Settings")]
    [SerializeField] private float _critMultiplier = 1.5f;

    [Header("Range Modifiers")]
    [SerializeField] private float _shortRangeDistance = 4.0f;
    [SerializeField] private float _shortRangeBonus = 0.1f;

    [Header("Environment Constants")]
    [SerializeField] private float _penaltyLowCover = 0.2f; // 추후 사용
    [SerializeField] private float _penaltyHighCover = 0.4f; // 추후 사용

    // 안전장치용 상수
    private const int DEFAULT_WEAPON_DAMAGE_MIN = 1;
    private const int DEFAULT_WEAPON_DAMAGE_MAX = 2;
    private const float DEFAULT_AIM = 0.7f;
    private const float DEFAULT_EVASION = 0.1f;
    private const float DEFAULT_CRIT_CHANCE = 0.1f;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<CombatManager>(ManagerScope.Scene);
    public UniTask Initialize(InitializationContext context) => UniTask.CompletedTask;

    /// <summary>
    /// 공격 로직 실행 (리팩토링 완료)
    /// </summary>
    public async UniTask<AttackResult> ExecuteAttack(Unit attacker, Unit target)
    {
        if (attacker?.Data == null || target?.Data == null) return AttackResult.Miss;

        // 1. 데이터 추출
        var weapon = attacker.Data.MainWeapon;
        var armor = target.Data.BodyArmor;

        // 2. 명중/회피 계산
        float hitChance = CalculateHitChance(attacker, target);

        if (UnityEngine.Random.value > hitChance)
        {
            Debug.Log($"[Combat] MISS! (Chance: {hitChance:P0})");
            ShowFeedback(target.transform.position, 0, false, true);
            return AttackResult.Miss;
        }

        // 3. 데미지 및 치명타 계산
        int rawDamage = (weapon != null)
            ? UnityEngine.Random.Range(weapon.Damage.Min, weapon.Damage.Max + 1)
            : UnityEngine.Random.Range(DEFAULT_WEAPON_DAMAGE_MIN, DEFAULT_WEAPON_DAMAGE_MAX + 1);

        // [수정] 방어력을 float로 받아옵니다. (CS0266 해결)
        // ArmorDataSO의 Defense 필드가 float여야 합니다.
        float defense = (armor != null) ? armor.DefenseTier : 0f;

        float critChance = attacker.Data.CritChance > 0 ? attacker.Data.CritChance : DEFAULT_CRIT_CHANCE;
        bool isCrit = UnityEngine.Random.value <= critChance;

        // [수정] 계산 과정을 전부 float로 처리하여 정밀도 유지
        float damageMultiplier = isCrit ? _critMultiplier : 1.0f;
        float damageAfterCrit = rawDamage * damageMultiplier;

        // (데미지 - 방어력) 계산
        float finalCalculation = damageAfterCrit - defense;

        // [수정] 최종 단계에서 int로 반올림 (최소 데미지 1 보장)
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalCalculation));

        // 4. 결과 적용
        Debug.Log($"[Combat] HIT! Dmg: {finalDamage} (Raw: {rawDamage}, Def: {defense:F2}, Calc: {finalCalculation:F2})");

        ShowFeedback(target.transform.position, finalDamage, isCrit, false);
        await target.TakeDamage(finalDamage);
        attacker.ConsumeAP(1);

        return isCrit ? AttackResult.Critical : AttackResult.Hit;
    }

    // 명중률 계산 로직 (거리 보정 등 포함)
    private float CalculateHitChance(Unit attacker, Unit target)
    {
        float baseAim = attacker.Data.Aim;      // 직접 접근
        float targetEvasion = target.Data.Evasion; // 직접 접근

        // 거리 보정
        float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
        float rangeBonus = (dist < _shortRangeDistance) ? _shortRangeBonus : 0f;

        // 엄폐 페널티 (TODO)
        float coverPenalty = 0f; 

        return Mathf.Clamp01((baseAim + rangeBonus) - (targetEvasion + coverPenalty));
    }

    private void ShowFeedback(Vector3 position, int damage, bool isCrit, bool isMiss)
    {
        if (ServiceLocator.TryGet(out DamageTextManager textManager))
        {
            textManager.ShowDamage(position, damage, isCrit, isMiss);
        }
    }
}