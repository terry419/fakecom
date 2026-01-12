using UnityEngine;

[CreateAssetMenu(fileName = "GlobalCombatSettings", menuName = "Data/Global/CombatSettings")]
public class GlobalCombatSettingsSO : ScriptableObject
{
    [Header("Tactical Rules (Environment)")]
    [Tooltip("부분 엄폐(Low Cover)가 제공하는 방어/회피 보너스")]
    public float LowCoverDefense = 20f;

    [Tooltip("완전 엄폐(High Cover)가 제공하는 방어/회피 보너스")]
    public float HighCoverDefense = 40f;

    [Tooltip("근접 사격 보너스가 적용되는 거리 한계 (타일 단위)")]
    public float CloseRangeThreshold = 4f;

    // 추후 추가 가능: 낙하 데미지, 고지대 명중 보정 등
}