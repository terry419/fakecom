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
    public float CritChance = 5f;

    [Header("4. Player Loadout")]
    public WeaponDataSO MainWeapon;
    public ArmorDataSO BodyArmor;

    [Header("5. Neural Sync")]
    [Tooltip("기본 생존 확률 (Assault: 5, Scout: 8, Sniper: 2)")]
    public float BaseSurvivalChance = 5f;
    [Tooltip("초기 싱크로율 (기본 100)")]
    public float BaseNeuralSync = 100f;
    [Tooltip("오버클럭 성공 기본 확률")]
    public float BaseOverclockChance = 5f;

}