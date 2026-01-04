using UnityEngine;
using System.Collections.Generic;

// [New] 더미 적 스펙 정의 (유연성 확보)
public class EnemySpawnerSpec
{
    public string RoleTag;
    public int Count;
    public int HP;
    public string NamePrefix; // 예: "Goblin", "Orc"

    public EnemySpawnerSpec(string tag, int count, int hp, string prefix)
    {
        RoleTag = tag;
        Count = count;
        HP = hp;
        NamePrefix = prefix;
    }
}

// [Fix] Config 확장 (Spec 리스트 포함)
public struct MissionDataFactoryConfig
{
    public int MaxPlayerCount;
    public string PlayerSpawnTag;
    public List<EnemySpawnerSpec> DummyEnemies; // 리스트로 변경하여 다중 타입 지원
}

public static class MissionDataFactory
{
    // 기본값 생성 메서드
    public static MissionDataFactoryConfig GetDefaultConfig()
    {
        return new MissionDataFactoryConfig
        {
            MaxPlayerCount = 3,
            PlayerSpawnTag = "Spawn_Player",
            DummyEnemies = new List<EnemySpawnerSpec>
            {
                // 기본적으로 1종류의 더미 적 추가
                new EnemySpawnerSpec("Spot_A", 2, 100, "Dummy_Grunt")
            }
        };
    }

    public static MissionDataSO CreateTestMission(MapEntry mapEntry, MissionDataFactoryConfig? config = null)
    {
        var cfg = config ?? GetDefaultConfig();
        var dummyMission = ScriptableObject.CreateInstance<MissionDataSO>();

        dummyMission.name = $"TestMission_{mapEntry.MapID}";
        dummyMission.MapDataRef = mapEntry.MapDataRef;

        dummyMission.MissionSettings = new MissionSettings
        {
            MissionName = mapEntry.MapID,
            Description = $"[Runtime Generated] {mapEntry.Description}",
            Type = MissionType.Exterminate
        };

        dummyMission.PlayerSettings = new PlayerRosterSettings
        {
            MaxPlayerCount = cfg.MaxPlayerCount,
            PlayerSpawnTag = cfg.PlayerSpawnTag
        };

        dummyMission.EnemySpawns = new List<EnemySpawnEntry>();

        // [Fix] Spec 리스트를 순회하며 더미 데이터 생성
        if (cfg.DummyEnemies != null)
        {
            foreach (var spec in cfg.DummyEnemies)
            {
                var dummyUnit = ScriptableObject.CreateInstance<EnemyUnitDataSO>();
                dummyUnit.name = $"{spec.NamePrefix}_Runtime";
                dummyUnit.MaxHP = spec.HP;
                // 필요한 경우 Mobility, Aim 등 추가 설정

                dummyMission.EnemySpawns.Add(new EnemySpawnEntry
                {
                    RoleTag = spec.RoleTag,
                    Count = spec.Count,
                    Unit = dummyUnit
                });
            }
        }

        return dummyMission;
    }
}