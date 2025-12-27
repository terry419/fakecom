using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Data/Item/Consumable")]
public class ConsumableDataSO : ScriptableObject
{
    [Header("1. Basic Info")]
    public string ItemID;
    public string DisplayName;
    public Sprite Icon; // [Hybrid] UI 표시용

    [Header("2. Effect Logic")]
    public ItemEffectType EffectType;

    [Tooltip("피해량, 회복량, 혹은 스탯 증가량")]
    public float EffectValue;

    [Tooltip("지속 턴 수 (0 = 즉시 발동, 1 이상 = 버프/장판 지속)")]
    public int Duration;

    [Header("3. Specific Targets")]
    [Tooltip("[Cure/Immunity] 치료하거나 막아줄 상태이상")]
    public StatusType TargetStatus;

    [Tooltip("[Buff] 강화할 스탯 종류")]
    public StatType TargetStat;

    [Header("4. Combat Specs")]
    [Tooltip("[Ammo Only] 탄약의 공격 등급 (T1~T5)")]
    public int AttackTier;

    [Tooltip("투척 사거리 (0 = 자신 사용)")]
    public int Range;

    [Tooltip("효과 범위 반경 (1.0 = 단일, 2.5 = 폭발 등)")]
    public float AreaRadius;

    [Header("5. Visuals")]
    public AssetReferenceGameObject VFX_Ref; // 폭발/사용 이펙트
}