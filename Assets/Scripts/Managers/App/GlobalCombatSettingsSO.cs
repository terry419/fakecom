using UnityEngine;

[CreateAssetMenu(fileName = "GlobalCombatSettings", menuName = "Data/Global/CombatSettings")]
public class GlobalCombatSettingsSO : ScriptableObject
{
    [Header("1. Cover Settings (Normalized 0.0 ~ 1.0)")]
    [Tooltip("부분 엄폐(Low Cover) 방어율 (GDD 5.3: 20 -> 0.2)")]
    [Range(0f, 1f)] public float LowCoverDefense = 0.2f;

    [Tooltip("완전 엄폐(High Cover) 방어율 (GDD 5.3: 40 -> 0.4)")]
    [Range(0f, 1f)] public float HighCoverDefense = 0.4f;

    [Tooltip("층수 차이당 엄폐 효율 감소율 (GDD 5.3: 층당 5%)")]
    [Range(0f, 1f)] public float HeightReductionFactor = 0.05f;

    [Tooltip("최소 엄폐 효율 보장값 (고지대 패널티 최대치 제한)")]
    [Range(0f, 1f)] public float MinHeightFactor = 0.8f;

    [Tooltip("최소 각도 계수 (기본 0). 엄폐물 측면/후면 공격 시 최소한의 엄폐 인정 여부.")]
    [Range(0f, 1f)] public float AngleFactorMin = 0f;

    [Header("2. Efficiency Settings (GDD 9.3)")]
    [Tooltip("공방 효율 공식의 분자/분모 상수 (GDD 기준: 2.0). 값이 클수록 등급 격차에 의한 페널티가 줄어듭니다.")]
    public float EfficiencyConstant = 2.0f;

}