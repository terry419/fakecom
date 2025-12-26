using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLootTable", menuName = "Data/Loot/LootTable")]
public class LootTableSO : ScriptableObject
{
    [System.Serializable]
    public struct LootEntry
    {
        public ConsumableDataSO ItemRef; // 드랍할 아이템
        [Range(0, 100)] public float DropChance; // 드랍 확률 (%)
    }

    [Header("Loot Configuration")]
    public List<LootEntry> DropList;
}