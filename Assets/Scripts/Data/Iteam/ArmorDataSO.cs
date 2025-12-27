using UnityEngine;

[CreateAssetMenu(fileName = "NewArmorData", menuName = "Data/Item/ArmorData")]
public class ArmorDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string ArmorID;
    public string ArmorName;
    public Sprite ArmorIcon; // [Hybrid] UI 아이콘

    [Header("2. Specs")]
    [Tooltip("방어 등급 (T1~T5). 공격 등급(T_Atk)과 비교하여 데미지 효율 결정.")]
    public int DefenseTier;

    [Tooltip("이동력 감소 패널티. (예: 1 입력 시 이동력 -1)")]
    public int MobilityPenalty;
}