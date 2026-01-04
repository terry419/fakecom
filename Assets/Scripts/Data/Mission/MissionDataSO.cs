using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

// [Fix] 삭제되었던 필수 정의 복구 (CS0246 해결)
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
    public EnemyUnitDataSO UnitData;
}

// [Refactor] UI 전용 데이터 분리
[System.Serializable]
public struct MissionUIMetadata
{
    [Tooltip("월드맵 버튼 위치 ID (예: Site_A). 시스템 식별용.")]
    public string LocationID;

    [Tooltip("UI 표시용 지역 이름 (예: Sector 7).")]
    public string LocationName;

    [Tooltip("맵 크기 (Small, Medium, Large, Huge)")]
    public MapSize MapSize;

    [Tooltip("맵 난이도)")]
    [Range(1, 10)]
    public int DifficultyLevel;

    [Tooltip("디자인 기준 예상 적 숫자. 실제 스폰과 다를 수 있음.")]
    public int EstimatedEnemyCount;

    [Tooltip("최대 출격 가능 인원.")]
    public int MaxSquadSize;
}

[System.Serializable]
public struct MissionSettings // [Fix] Definition 대신 Settings로 이름 통일
{
    public string MissionName;
    public MissionType Type;
    public int TimeLimit;
}

[CreateAssetMenu(fileName = "NewMissionData", menuName = "Data/Map/MissionData")]
public class MissionDataSO : ScriptableObject
{
    [Header("1. Map Reference")]
    public AssetReferenceT<MapDataSO> MapDataRef;

    [Header("1.5. UI Metadata")]
    public MissionUIMetadata UI;
    public MissionSettings MissionSettings;

    [Header("2. Definition")]
    public MissionDefinition Definition; // [Refactor] Settings -> Definition

    [Header("3. Configs")]
    public PlayerMissionConfig PlayerConfig;

    [Header("4. Spawns")]
    public List<EnemySpawnDef> EnemySpawns = new List<EnemySpawnDef>();
    public List<NeutralSpawnDef> NeutralSpawns = new List<NeutralSpawnDef>();

    [Header("5. Rewards")]
    public List<ItemDataSO> Rewards = new List<ItemDataSO>();
}