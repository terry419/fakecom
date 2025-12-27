using UnityEngine;
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

    [Range(0f, 100f)]
    public float CritChance = 5f;

    [Header("4. Player Loadout")]
    [Tooltip("기본 지급 주무기")]
    public WeaponDataSO MainWeapon;

    [Tooltip("기본 지급 방어구")]
    public ArmorDataSO BodyArmor;

    // [삭제됨] StartingAmmo, ExtraItems 
    // -> 모든 소모품/탄약은 공용 인벤토리(상점 구매분)를 사용함

    [Header("5. Neural Sync")]
    public float BaseSurvivalChance = 5f;
    public float BaseNeuralSync = 100f;
    public float BaseOverclockChance = 5f;
}