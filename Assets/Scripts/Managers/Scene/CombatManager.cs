using UnityEngine;
using Cysharp.Threading.Tasks;

public enum AttackResult
{
    Miss,
    Hit,
    Critical
}

public class CombatManager : MonoBehaviour, IInitializable
{
    [Header("Combat Balance Settings")]
    [SerializeField] private float _critMultiplier = 1.5f;

    [Header("Range Modifiers")]
    [SerializeField] private float _shortRangeDistance = 4.0f;
    [SerializeField] private float _shortRangeBonus = 0.1f;

    // 안전장치 상수
    private const int DEFAULT_WEAPON_DAMAGE_MIN = 1;
    private const int DEFAULT_WEAPON_DAMAGE_MAX = 2;
    private const float DEFAULT_CRIT_CHANCE = 0.1f;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<CombatManager>(ManagerScope.Scene);
    public UniTask Initialize(InitializationContext context) => UniTask.CompletedTask;

    public async UniTask<AttackResult> ExecuteAttack(Unit attacker, Unit target)
    {
        if (attacker?.Data == null || target?.Data == null) return AttackResult.Miss;

        var weapon = attacker.Data.MainWeapon;
        var armor = target.Data.BodyArmor;

        // 1. 명중/회피 계산
        float hitChance = CalculateHitChance(attacker, target);

        // 명중 굴림
        if (UnityEngine.Random.value > hitChance)
        {
            Debug.Log($"[Combat] MISS! (Chance: {hitChance:P0})");
            ShowFeedback(target.transform.position, 0, false, true);

            // [중요] 빗나갔어도 공격 행동 소모
            attacker.MarkAsAttacked();
            return AttackResult.Miss;
        }

        // 2. 데미지 및 치명타 계산
        int rawDamage = (weapon != null)
            ? UnityEngine.Random.Range(weapon.Damage.Min, weapon.Damage.Max + 1)
            : UnityEngine.Random.Range(DEFAULT_WEAPON_DAMAGE_MIN, DEFAULT_WEAPON_DAMAGE_MAX + 1);

        float defense = (armor != null) ? armor.DefenseTier : 0f;
        float critChance = attacker.Data.CritChance > 0 ? attacker.Data.CritChance : DEFAULT_CRIT_CHANCE;
        bool isCrit = UnityEngine.Random.value <= critChance;

        float damageMultiplier = isCrit ? _critMultiplier : 1.0f;
        float damageAfterCrit = rawDamage * damageMultiplier;
        float finalCalculation = damageAfterCrit - defense;
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(finalCalculation));

        // 3. 결과 적용 (UI -> HP -> 상태)
        Debug.Log($"[Combat] HIT! Dmg: {finalDamage} (Raw: {rawDamage}, Def: {defense:F2})");

        ShowFeedback(target.transform.position, finalDamage, isCrit, false);
        await target.TakeDamage(finalDamage);

        // [중요] 공격 행동 소모
        attacker.MarkAsAttacked();

        return isCrit ? AttackResult.Critical : AttackResult.Hit;
    }

    private float CalculateHitChance(Unit attacker, Unit target)
    {
        float baseAim = attacker.Data.Aim;
        float targetEvasion = target.Data.Evasion;

        float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
        float rangeBonus = (dist < _shortRangeDistance) ? _shortRangeBonus : 0f;
        float coverPenalty = 0f;

        return Mathf.Clamp01((baseAim + rangeBonus) - (targetEvasion + coverPenalty));
    }

    private void ShowFeedback(Vector3 position, int damage, bool isCrit, bool isMiss)
    {
        // ServiceLocator를 통해 DamageTextManager 호출 (제공해주신 스크립트 대응)
        if (ServiceLocator.TryGet(out DamageTextManager textManager))
        {
            textManager.ShowDamage(position, damage, isCrit, isMiss);
        }
    }
}