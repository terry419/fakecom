using UnityEngine;

// 공격 결과 타입
public enum AttackResultType { Miss, Graze, Hit, Critical }

// 실행 순서 (Modifier Priority)
public static class ModifierPriority
{
    // [명중률 파이프라인] 0 ~ 999
    public const int HC_Base = 0;          // 기본 명중률 (스탯 + 무기)
    public const int HC_Environment = 200; // 엄폐, 고지대
    public const int HC_Abilities = 500;   // 스킬/버프
    public const int HC_Final = 900;       // 최종 보정 (Clamp)

    // [데미지 파이프라인] 1000 ~ 1999
    public const int DMG_Base = 1000;       // 기본 무기+탄약 피해
    public const int DMG_Efficiency = 1100; // 거리/방어구 효율
    public const int DMG_Crit = 1200;       // 치명타/배율
    public const int DMG_FinalClamp = 1999; // 최종 제한
}