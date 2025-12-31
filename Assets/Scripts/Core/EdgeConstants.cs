using UnityEngine;

public static class EdgeConstants
{
    // ========================================================================
    // 1. 내구도 (HP)
    // ========================================================================
    public const float HP_WALL = 100f;
    public const float HP_WINDOW = 30f;
    public const float HP_DOOR = 50f;

    // 기둥 파괴 임계값 (GDD: 50% 이하 Broken, 0% Debris)
    public const float PILLAR_BROKEN_THRESHOLD = 0.5f;

    // ========================================================================
    // 2. 엄폐율 (GDD: 0.0 ~ 1.0 범위)
    // ========================================================================
    public const float BASE_COVER_HIGH = 0.4f;  // 완전엄폐 40%
    public const float BASE_COVER_LOW = 0.2f;   // 반엄폐 20%

    // ========================================================================
    // 3. 높이 계수 공식 상수 (Height Factor)
    // Formula: max(0.80, 1.0 - (Diff * 0.05))
    // ========================================================================
    public const float HEIGHT_COEF_MIN = 0.80f;
    public const float HEIGHT_COEF_STEP = 0.05f;
}