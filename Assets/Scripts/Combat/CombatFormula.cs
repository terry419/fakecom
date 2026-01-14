using UnityEngine;

/// <summary>
/// [GDD 5.3, 9.3, 9.4] 전투 핵심 공식을 처리하는 순수 연산 클래스.
/// [단위 규약] 모든 확률/비율(Aim, Evasion, Cover)은 0.0 ~ 1.0 (Normalized) 범위를 따릅니다.
/// </summary>
public static class CombatFormula
{
    // ========================================================================
    // 1. 구조체 정의
    // ========================================================================
    public struct HitChanceContext
    {
        public float FinalHitChance;    // 최종 명중률 (0.0 ~ 1.0)
        public float BaseAim;           // 공격자 명중률 (0.0 ~ 1.0)
        public float RangeBonus;        // 거리 보정 (0.0 ~ 1.0)
        public float TargetEvasion;     // 회피율 (0.0 ~ 1.0)
        public float FinalCoverRating;  // 최종 엄폐율 (0.0 ~ 1.0)

        // UI/Debug Info
        public float AngleFactor;
        public float HeightFactor;
    }

    // ========================================================================
    // 2. 엄폐 공식 (Cover Formula) - GDD 5.3
    // ========================================================================

    /// <summary>
    /// 최종 엄폐율을 계산합니다. (0.0 ~ 1.0)
    /// </summary>
    public static float CalculateCoverRating(
            CoverType coverType,
            Vector3 coverNormal,
            Vector3 attackerPos,
            Vector3 targetPos,
            int attackerLayer,
            int targetLayer,
            bool isTargetIndoor,
            GlobalCombatSettingsSO settings)
    {
        if (settings == null)
        {
            Debug.LogError("[CombatFormula] GlobalCombatSettingsSO is null! Defaulting cover to 0.");
            return 0f;
        }

        if (coverType == CoverType.None) return 0f;

        // 1. Base Cover Value
        float baseCover = (coverType == CoverType.High) ? settings.HighCoverDefense : settings.LowCoverDefense;

        // 2. Angle Factor (벡터 내적)
        Vector3 attackDirection = (attackerPos - targetPos).normalized;
        float dot = Vector3.Dot(coverNormal, attackDirection);

        // [Fix] 하드코딩 0f 제거 -> 설정값 사용
        float angleFactor = Mathf.Max(settings.AngleFactorMin, dot);

        // 3. Height Factor (고저차)
        int deltaH = attackerLayer - targetLayer;
        float heightFactor = 1.0f;

        if (isTargetIndoor && coverType == CoverType.High)
        {
            heightFactor = 1.0f;
        }
        else if (deltaH > 0)
        {
            heightFactor = Mathf.Max(settings.MinHeightFactor, 1.0f - (deltaH * settings.HeightReductionFactor));
        }

        return baseCover * angleFactor * heightFactor;
    }

    // ========================================================================
    // 3. 공방 효율 공식 (Efficiency) - GDD 9.3
    // ========================================================================

    /// <summary>
    /// 공격/방어 등급 격차에 따른 데미지 효율 계산.
    /// 공식: EfficiencyConstant / (Max(0, Gap) + EfficiencyConstant)
    /// [예시] (Constant = 2.0 가정)
    /// - Gap = 0: efficiency = 2.0 / 2.0 = 1.0 (100%)
    /// - Gap = 2: efficiency = 2.0 / 4.0 = 0.5 (50%)
    /// </summary>
    public static float CalculateEfficiency(float attackTier, float defenseTier, GlobalCombatSettingsSO settings)
    {
        float constant = settings != null ? settings.EfficiencyConstant : 2.0f;

        float gap = defenseTier - attackTier;
        float denominator = Mathf.Max(0f, gap) + constant;

        if (denominator == 0f) return 1.0f;

        return constant / denominator;
    }

    // ========================================================================
    // 4. 명중률 공식 (Hit Chance) - GDD 9.4
    // ========================================================================

    /// <summary>
    /// 최종 명중률 계산. 모든 입력값은 0.0 ~ 1.0 범위여야 합니다.
    /// </summary>
    public static HitChanceContext CalculateHitChance(
        float attackerAim,      // 0.0 ~ 1.0
        float rangeBonus,       // 0.0 ~ 1.0
        float targetEvasion,    // 0.0 ~ 1.0
        float finalCoverRating, // 0.0 ~ 1.0
        float angleFactorInfo,
        float heightFactorInfo)
    {
        // 공식: (명중 + 거리보정) - (회피 + 엄폐)
        float hitChance = (attackerAim + rangeBonus) - (targetEvasion + finalCoverRating);
        hitChance = Mathf.Clamp01(hitChance);

        return new HitChanceContext
        {
            FinalHitChance = hitChance,
            BaseAim = attackerAim,
            RangeBonus = rangeBonus,
            TargetEvasion = targetEvasion,
            FinalCoverRating = finalCoverRating,
            AngleFactor = angleFactorInfo,
            HeightFactor = heightFactorInfo
        };
    }
}