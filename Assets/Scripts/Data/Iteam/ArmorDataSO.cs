using UnityEngine;

[CreateAssetMenu(fileName = "NewArmor", menuName = "Data/Item/Armor")]
public class ArmorDataSO : ItemDataSO
{

    [Header("2. Specs")]
    [Tooltip("방어 등급 (T1~T5). 공격 등급(T_Atk)과 비교하여 데미지 효율 결정.")]
    public int DefenseTier;

    [Tooltip("이동력 감소 패널티. (예: 1 입력 시 이동력 -1)")]
    public int MobilityPenalty;

    [Header("3. Durability")]
    public int MaxDurability;

    private void OnEnable()
    {
        Type = ItemType.Armor;
        MaxStack = 1;
    }

}