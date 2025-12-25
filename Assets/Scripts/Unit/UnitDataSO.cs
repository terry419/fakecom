using UnityEngine;

[CreateAssetMenu(fileName = "NewUnitData", menuName = "Data/Unit")]
public class UnitDataSO : ScriptableObject
{
    [Header("Identity")]
    public string UnitID;
    public string UnitName;
    public ClassType Role;

    [Header("Visual")]
    public GameObject ModelPrefab;

    [Header("Stats")]
    public BaseStatsStruct BaseStats;
}

[System.Serializable]
public struct BaseStatsStruct
{
    public int MaxHP;
    public int Mobility;
    public int Agility;
    public int Aim;
    public int Evasion;
    [Range(0f, 100f)] public float CritChance;
}