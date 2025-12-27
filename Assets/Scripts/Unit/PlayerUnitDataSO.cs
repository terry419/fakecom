using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewPlayerUnit", menuName = "Data/Unit/PlayerUnit")]
public class PlayerUnitDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string UnitID;
    public string UnitName;
    public ClassType Role;

    [Header("2. Visual")]
    public AssetReferenceGameObject ModelPrefab;

    [Header("3. Base Stats")]
    public int MaxHP;
    public int Mobility;
    public int Agility;
    public int Aim;
    public int Evasion;

    [Header("4. Player Loadout")]
    public WeaponDataSO MainWeapon;
    public ArmorDataSO BodyArmor;
    public ConsumableDataSO StartingAmmo;
    public List<ConsumableDataSO> ExtraItems;
}