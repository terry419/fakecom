using UnityEngine;

public enum ItemType { None, Ammo, Consumable, Grenade, Resource, Weapon, Armor }

/// <summary>
/// 모든 인벤토리 아이템의 최상위 부모 클래스입니다.
/// </summary>
public abstract class ItemDataSO : ScriptableObject
{
    [Header("1. Shared Identity")]
    public string ItemID;           // 통합 ID (기존 AmmoID, WeaponID 대체)
    public string ItemName;         // 통합 이름 (기존 DisplayName, AmmoName 대체)
    public Sprite ItemIcon;         // 통합 아이콘 (기존 Icon, WeaponIcon 대체)
    public ItemType Type;           // 아이템 타입 구분

    [TextArea] public string Description; // 아이템 설명

    [Header("2. Economy")]
    public int Price_Buy;           // 구매가
    public int Price_Sell;          // 판매가 (기본값: 구매가의 30%)
    public int MaxStack = 99;       // 최대 스택 수
}