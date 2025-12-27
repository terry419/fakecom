using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Data/Item/Consumable")]
public class ConsumableDataSO : ItemDataSO // [변경] 상속 변경
{
    public override ItemType Type => ItemType.Consumable;

    [Header("2. Effect Logic (Self/Team Only)")]
    [Tooltip("사용 용도 (Heal, BuffStat, CureStatus, GrantImmunity, RestoreNS)")]
    public ConsumableEffectType EffectType;

    [Tooltip("회복량(Heal/RestoreNS) 혹은 강화량(BuffStat)")]
    public float Value;

    [Tooltip("지속 턴 (Buff/Immunity 전용. 0 = 즉발)")]
    public int Duration;

    [Header("3. Targets")]
    [Tooltip("[Cure/Immunity] 치료하거나 면역할 상태이상")]
    public StatusType TargetStatus;

    [Tooltip("[Buff] 강화할 스탯 (Mobility, Aim 등)")]
    public StatType TargetStat;

    [Header("4. Visuals")]
    [Tooltip("사용 시 이펙트")]
    public AssetReferenceGameObject UseVFX;

}