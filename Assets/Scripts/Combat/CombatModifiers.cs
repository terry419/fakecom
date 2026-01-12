using UnityEngine;

// 1. 기본 명중률 계산 (확정된 코드 유지)
public class BaseHitChanceModifier : IHitChanceModifier
{
    public int Priority => ModifierPriority.HC_Base;
    public bool CanApply(HitChanceContext context) => true;

    public float Apply(float currentHitChance, HitChanceContext context)
    {
        float aim = context.Attacker.Data.Aim;
        float evasion = context.Target.Data.Evasion;
        float baseChance = Mathf.Clamp(aim - evasion, 0f, 100f);

        // 거리 효율 (Curve)
        float distEfficiency = 1.0f;
        if (context.Weapon != null && context.Weapon.AccuracyCurveData != null)
        {
            distEfficiency = context.Weapon.AccuracyCurveData.Evaluate(context.Distance);
        }

        // 초기값 할당
        return baseChance * distEfficiency;
    }
}

// 2. 환경(엄폐/고지대) 명중률 보정 (GDD 5.3 공식 적용)
public class EnvironmentHitChanceModifier : IHitChanceModifier
{
    private readonly GlobalCombatSettingsSO _settings;

    public EnvironmentHitChanceModifier(GlobalCombatSettingsSO settings)
    {
        _settings = settings;
    }

    public int Priority => ModifierPriority.HC_Environment;
    public bool CanApply(HitChanceContext context) => _settings != null;

    public float Apply(float currentHitChance, HitChanceContext context)
    {
        // [1] 기본 엄폐값 (BaseCover)
        // Context에서 MapManager가 판정한 CoverType을 가져옴 (문자열 키 사용)
        string coverType = context.GetData<string>(CombatDataKeys.CoverType);
        float baseCover = 0f;

        if (coverType == "Half") baseCover = _settings.LowCoverDefense; // 20
        else if (coverType == "Full") baseCover = _settings.HighCoverDefense; // 40

        // 엄폐물이 없으면 보정 없음
        if (baseCover <= 0f) return currentHitChance;

        // [2] 고지대 계수 (Height Factor)
        // 공식: F_Height = Max(0.8, 1.0 - (층수차이 * 0.05))
        float yDiff = context.Attacker.transform.position.y - context.Target.transform.position.y;
        float levelDiff = Mathf.Max(0f, yDiff / GridUtils.LEVEL_HEIGHT); // GridUtils 상수 사용

        float fHeight = Mathf.Max(0.8f, 1.0f - (levelDiff * 0.05f));

        // [3] 각도 계수 (Angle Factor)
        // 공식: F_Angle = Max(0, Dot(CoverDir, AttackDir))
        // 엄폐 방향이 Context에 없으면, 정면(1.0)으로 가정하거나 계산.
        // 여기서는 공격 벡터와 엄폐물 벡터의 내적이 필요함.
        // *우선 구현*: 엄폐 판정이 났다는 건 각도가 유효하다는 뜻이므로 1.0에서 시작하되, 측면이면 감소.
        float fAngle = 1.0f;
        if (context.GetData<bool>(CombatDataKeys.IsFlanked, false))
        {
            fAngle = 0f; // 측면 공격 성공 시 엄폐 무효화
        }

        // [4] 최종 엄폐 방어력
        float finalCover = baseCover * fAngle * fHeight;

        context.AddLog($"[Env] Base({baseCover}) * H_Factor({fHeight:F2}) = -{finalCover:F1}%");

        return currentHitChance - finalCover;
    }
}

// 3. 기본 데미지 계산 (확정된 코드 유지)
public class BaseDamageModifier : IDamageModifier
{
    public int Priority => ModifierPriority.DMG_Base;
    public bool CanApply(DamageContext context) => context.Weapon != null;

    public float Apply(float currentDamage, DamageContext context)
    {
        // Miss면 데미지 없음 (예측 모드 아닐 때)
        if (!context.IsPrediction && context.Result == AttackResultType.Miss)
            return 0f;

        int minDmg = context.Weapon.Damage.Min;
        int maxDmg = context.Weapon.Damage.Max;

        // 탄약 데미지 합산 (데이터 구조 확인 시 추가)
        // if (context.Ammo != null) { ... }

        if (context.IsPrediction)
        {
            context.CurrentMinDamage += minDmg;
            context.CurrentMaxDamage += maxDmg;
            return currentDamage + ((minDmg + maxDmg) * 0.5f);
        }
        else
        {
            int rolled = Random.Range(minDmg, maxDmg + 1);
            return currentDamage + rolled;
        }
    }
}