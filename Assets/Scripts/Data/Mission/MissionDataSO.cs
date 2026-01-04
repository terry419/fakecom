using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

[System.Serializable]
public struct MissionSettings
{
    public string MissionName;
    [TextArea] public string Description;
    public MissionType Type;
}

[System.Serializable]
public struct PlayerRosterSettings
{
    public int MaxPlayerCount;      // 출격 가능 인원
    public string PlayerSpawnTag;   // 맵 에디터의 RoleTag와 매칭 (예: "Spawn_Player")
}

[System.Serializable]
public struct EnemySpawnEntry
{
    public EnemyUnitDataSO Unit;    // 적 유닛 데이터
    public string RoleTag;          // 스폰 위치 태그 (예: "Spot_A")
    public int Count;               // 수량
}

[CreateAssetMenu(fileName = "NewMissionData", menuName = "Data/Map/MissionData")]
public class MissionDataSO : ScriptableObject
{
    [Header("1. Map Reference")]
    public AssetReferenceT<MapDataSO> MapDataRef;

    [Header("2. Settings")]
    public MissionSettings MissionSettings;
    public PlayerRosterSettings PlayerSettings;

    [Header("3. Spawns")]
    public List<EnemySpawnEntry> EnemySpawns = new List<EnemySpawnEntry>();

    [Header("4. Rewards")]
    public List<ItemDataSO> Rewards = new List<ItemDataSO>();
}