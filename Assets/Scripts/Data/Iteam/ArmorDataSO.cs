using UnityEngine;

[CreateAssetMenu(fileName = "NewArmor", menuName = "Data/Item/Armor")]
public class ArmorDataSO : ItemDataSO
{
    public override ItemType Type => ItemType.Armor;

    [Header("2. Specs")]
    [Tooltip("방어 등급 (T1~T5). 공격 등급(T_Atk)과 비교하여 데미지 효율 결정.")]
    [Range(0f, 5f)]
    public float DefenseTier;

    [Tooltip("이동력 감소 패널티. (예: 1 입력 시 이동력 -1)")]
    public int MobilityPenalty;

    [Header("3. Durability")]
    [Tooltip("최대 내구도")]
    public float MaxDurability = 100f;

    private void OnEnable()
    {
        MaxStack = 1;
    }

}