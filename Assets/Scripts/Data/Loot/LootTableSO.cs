using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Data/Loot/LootTable")]
public class LootTableSO : ScriptableObject
{
    [System.Serializable]
    public struct LootEntry
    {
        [Tooltip("드랍할 아이템 (Ammo, Grenade, Consumable, Weapon 등)")]
        public ItemDataSO ItemRef;

        [Range(0, 100)]
        [Tooltip("개별 획득 확률 (독립 시행). 예: 30 = 30% 확률로 획득.")]
        public float DropChance;
    }

    [Header("Independent Loot Rolls")]
    [Tooltip("각 아이템마다 주사위를 따로 굴립니다. 운이 좋으면 목록의 모든 아이템을 얻을 수도 있습니다.")]
    public List<LootEntry> DropList;
}