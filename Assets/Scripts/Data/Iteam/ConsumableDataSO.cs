using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Data/Item/Consumable")]
public class ConsumableDataSO : ScriptableObject
{
    [Header("1. Basic Info")]
    public string ItemID;
    public string DisplayName;
    public Sprite Icon;

    [Header("2. Effect Logic (Self/Team Only)")]
    [Tooltip("아이템의 주 용도 (Heal, BuffStat, CureStatus, GrantImmunity, RestoreNS)")]
    public ItemEffectType EffectType;

    [Tooltip("회복량(Heal/RestoreNS) 또는 스탯 상승량(BuffStat)")]
    public float Value;

    [Tooltip("지속 턴 수 (Buff/Immunity일 때만 사용. 0 = 즉시)")]
    public int Duration;

    [Header("3. Targets")]
    [Tooltip("[Cure/Immunity] 치료하거나 예방할 상태이상 종류")]
    public StatusType TargetStatus;

    [Tooltip("[Buff] 강화할 스탯 종류 (Mobility, Aim 등)")]
    public StatType TargetStat;

    [Header("4. Visuals")]
    [Tooltip("사용 시 재생할 이펙트")]
    public AssetReferenceGameObject UseVFX;
}