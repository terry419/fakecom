using UnityEngine;

/// <summary>
/// [Phase 2 Final Fix] 실제 계산 로직 모듈
/// - GDD 9.0 공식 적용 (Base -> Efficiency -> Crit -> Clamp)
/// - 예측 모드(IsPrediction) 지원
/// </summary>

// Step 1: 기본 피해
public class BaseDamageModifier : IDamageModifier
{
    public int Priority => ModifierPriority.DMG_Base;
    public bool CanApply(DamageContext context) => context.Weapon != null;

    public float Apply(float currentDamage, DamageContext context)
    {
        if (context.Result == AttackResultType.Miss) return 0f;

        float min = context.Weapon.Damage.Min;
        float max = context.Weapon.Damage.Max;
        float rolled;

        if (context.IsPrediction)
        {
            // [UI] 예측 시 범위 양끝 설정 및 평균값 반환
            context.CurrentMinDamage = min;
            context.CurrentMaxDamage = max;
            rolled = (min + max) / 2f;
        }
        else
        {
            // [Combat] 실전은 랜덤
            rolled = Random.Range(min, max + 1);
            context.CurrentMinDamage = rolled;
            context.CurrentMaxDamage = rolled;
            context.AddLog($"[Base] Rolled: {rolled} ({min}~{max})");
        }

        // [Critical Fix] 이전 값(버프 등)을 덮어쓰지 않고 더함
        return currentDamage + rolled;
    }
}

// Step 3: 공방 효율 (GDD 9.3)
public class TierEfficiencyModifier : IDamageModifier
{
    public int Priority => ModifierPriority.DMG_Efficiency;
    public bool CanApply(DamageContext context) => context.Target?.Data?.BodyArmor != null;

    public float Apply(float currentDamage, DamageContext context)
    {
        // 탄약 공격등급 vs 타겟 방어등급
        float tDef = context.Target.Data.BodyArmor.DefenseTier;
        float tAtk = (context.Ammo != null) ? context.Ammo.AttackTier : 1.0f;

        float gap = tDef - tAtk;
        float efficiency = 2.0f / (Mathf.Max(0f, gap) + 2.0f);

        // 범위와 현재값 모두에 효율 적용
        context.CurrentMinDamage *= efficiency;
        context.CurrentMaxDamage *= efficiency;

        float final = currentDamage * efficiency;
        context.AddLog($"[Efficiency] Gap:{gap} (x{efficiency:F2}) -> {final:F1}");

        return final;
    }
}

// Step 4: 치명타 및 결과 배율
public class CriticalDamageModifier : IDamageModifier
{
    public int Priority => ModifierPriority.DMG_Crit;
    public bool CanApply(DamageContext context) => context.Result != AttackResultType.Miss;

    public float Apply(float currentDamage, DamageContext context)
    {
        float multiplier = 1.0f;
        if (context.Result == AttackResultType.Graze) multiplier = 0.75f;
        else if (context.Result == AttackResultType.Critical) multiplier = context.Weapon.CritBonus;

        context.CurrentMinDamage *= multiplier;
        context.CurrentMaxDamage *= multiplier;

        return currentDamage * multiplier;
    }
}

// Step 5: 최종 정수 처리
public class FinalDamageClampModifier : IDamageModifier
{
    public int Priority => ModifierPriority.DMG_FinalClamp;
    public bool CanApply(DamageContext context) => true;

    public float Apply(float currentDamage, DamageContext context)
    {
        if (context.Result == AttackResultType.Miss) return 0f;

        // 소수점 버림 및 최소 1 보장
        context.CurrentMinDamage = Mathf.Max(1, Mathf.Floor(context.CurrentMinDamage));
        context.CurrentMaxDamage = Mathf.Max(1, Mathf.Floor(context.CurrentMaxDamage));

        float final = Mathf.Max(1, Mathf.Floor(currentDamage));
        context.AddLog($"[Final] {final} (Range: {context.CurrentMinDamage}~{context.CurrentMaxDamage})");

        return final;
    }
}