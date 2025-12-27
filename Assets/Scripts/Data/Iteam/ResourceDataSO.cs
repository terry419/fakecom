using UnityEngine;

public enum Rarity { Common, Rare, Epic, Legendary }

[CreateAssetMenu(fileName = "NewResource", menuName = "Data/Item/Resource")]
public class ResourceDataSO : ItemDataSO
{
    [Header("Resource Info")]
    public Rarity RarityTier;

    [Tooltip("True: 전투 종료 후 자동 환불 / False: 창고 보관")]
    public bool IsAutoSell;

    private void OnEnable() => Type = ItemType.Resource;
}