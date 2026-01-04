using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

[System.Serializable]
public struct PlayerSpawnSlot
{
    [Tooltip("맵 에디터에 배치된 타일의 RoleTag (예: Spawn_Player_1)")]
    public string RoleTag;
}

[System.Serializable]
public class PlayerMissionConfig
{
    [Header("Spawn Configuration")]
    [Tooltip("참여 가능한 최대 인원수만큼 슬롯을 생성하고 태그를 지정하세요.")]
    public List<PlayerSpawnSlot> SpawnSlots = new List<PlayerSpawnSlot>();

    [Header("Default Roster")]
    [Tooltip("UI 선택 없이 바로 테스트할 때 사용할 기본 유닛들")]
    public List<PlayerUnitDataSO> DefaultSquad = new List<PlayerUnitDataSO>();
}

[System.Serializable]
public struct EnemySpawnDef
{
    public string RoleTag;
    public EnemyUnitDataSO UnitData;
}

[System.Serializable]
public struct NeutralSpawnDef
{
    public string RoleTag;
    // 중립 유닛용 SO가 따로 없다면 UnitDataSO 혹은 EnemyUnitDataSO 재사용
    public EnemyUnitDataSO UnitData;
}

[CreateAssetMenu(fileName = "NewMissionData", menuName = "Data/Map/MissionData")]
public class MissionDataSO : ScriptableObject
{
    [Header("1. Map Reference")]
    public AssetReferenceT<MapDataSO> MapDataRef;

    [Header("2. Settings")]
    public MissionSettings MissionSettings;

    // [Fix] 주인님 지시대로 PlayerSettings 통합 (슬롯 + 로스터)
    public PlayerMissionConfig PlayerConfig;

    [Header("3. Spawns")]
    public List<EnemySpawnDef> EnemySpawns = new List<EnemySpawnDef>();

    // [Fix] 중립 유닛(Neutral) 추가
    public List<NeutralSpawnDef> NeutralSpawns = new List<NeutralSpawnDef>();

    [Header("4. Rewards")]
    public List<ItemDataSO> Rewards = new List<ItemDataSO>();
}