using UnityEngine;
using System.Collections.Generic;

// [Fix] 누락되었던 PlayerSpawnerSpec 정의 추가
public class PlayerSpawnerSpec
{
    public string UnitName;
    public int HP;
}

public class EnemySpawnerSpec
{
    public string RoleTag;
    public int Count;
    public int HP;
    public string NamePrefix;

    public EnemySpawnerSpec(string tag, int count, int hp, string prefix)
    {
        RoleTag = tag; Count = count; HP = hp; NamePrefix = prefix;
    }
}

public struct MissionDataFactoryConfig
{
    public int MaxPlayerCount;
    public string PlayerSpawnTag;

    public List<EnemySpawnerSpec> DummyEnemies;
    public bool AddDummyEnemies;

    public bool AddDummyNeutrals;
    public List<EnemySpawnerSpec> DummyNeutrals;

    public bool AddDummyPlayers;
    public List<PlayerSpawnerSpec> DummyPlayers;
}

public static class MissionDataFactory
{
    public static MissionDataFactoryConfig GetDefaultConfig()
    {
        return new MissionDataFactoryConfig
        {
            MaxPlayerCount = 3,
            PlayerSpawnTag = "Spawn_Player",

            AddDummyEnemies = true,
            DummyEnemies = new List<EnemySpawnerSpec>
            {
                new EnemySpawnerSpec("Spot_A", 2, 100, "Dummy_Grunt")
            },

            AddDummyPlayers = true,
            DummyPlayers = new List<PlayerSpawnerSpec>
            {
                new PlayerSpawnerSpec { UnitName = "Hero_Alpha", HP = 150 },
                new PlayerSpawnerSpec { UnitName = "Sniper_Bravo", HP = 100 }
            },

            AddDummyNeutrals = false,
            DummyNeutrals = new List<EnemySpawnerSpec>()
        };
    }

    public static MissionDataSO CreateTestMission(MapEntry mapEntry, MissionDataFactoryConfig? config = null)
    {
        var cfg = config ?? GetDefaultConfig();
        var dummyMission = ScriptableObject.CreateInstance<MissionDataSO>();

        dummyMission.MapDataRef = mapEntry.MapDataRef;
        dummyMission.MissionSettings = new MissionSettings
        {
            MissionName = mapEntry.MapID,
            Type = MissionType.Exterminate
        };

        // --------------------------------------------------------
        // Player Config
        // --------------------------------------------------------
        dummyMission.PlayerConfig = new PlayerMissionConfig();
        for (int i = 0; i < cfg.MaxPlayerCount; i++)
        {
            dummyMission.PlayerConfig.SpawnSlots.Add(new PlayerSpawnSlot { RoleTag = cfg.PlayerSpawnTag });
        }

        if (cfg.AddDummyPlayers && cfg.DummyPlayers != null)
        {
            foreach (var spec in cfg.DummyPlayers)
            {
                // [Fix] 개별 인스턴스 생성
                var pData = ScriptableObject.CreateInstance<PlayerUnitDataSO>();
                pData.name = spec.UnitName;
                pData.BaseStats = new UnitStatBlock { MaxHP = spec.HP, Mobility = 6, Aim = 75 };
                dummyMission.PlayerConfig.DefaultSquad.Add(pData);
            }
        }

        // --------------------------------------------------------
        // Enemy Spawns (데이터 공유 방지)
        // --------------------------------------------------------
        dummyMission.EnemySpawns = new List<EnemySpawnDef>();
        if (cfg.AddDummyEnemies && cfg.DummyEnemies != null)
        {
            foreach (var spec in cfg.DummyEnemies)
            {
                for (int i = 0; i < spec.Count; i++)
                {
                    // [Fix] 루프 안에서 CreateInstance -> 독립 데이터 보장
                    var eData = ScriptableObject.CreateInstance<EnemyUnitDataSO>();
                    eData.name = $"{spec.NamePrefix}_{i}";
                    eData.MaxHP = spec.HP;

                    dummyMission.EnemySpawns.Add(new EnemySpawnDef
                    {
                        RoleTag = spec.RoleTag,
                        UnitData = eData
                    });
                }
            }
        }

        // --------------------------------------------------------
        // Neutral Spawns (데이터 공유 방지)
        // --------------------------------------------------------
        dummyMission.NeutralSpawns = new List<NeutralSpawnDef>();
        if (cfg.AddDummyNeutrals && cfg.DummyNeutrals != null)
        {
            foreach (var spec in cfg.DummyNeutrals)
            {
                for (int i = 0; i < spec.Count; i++)
                {
                    // [Fix] 루프 안에서 CreateInstance
                    var nData = ScriptableObject.CreateInstance<EnemyUnitDataSO>();
                    nData.name = $"Neutral_{spec.NamePrefix}_{i}";
                    nData.MaxHP = spec.HP;

                    dummyMission.NeutralSpawns.Add(new NeutralSpawnDef
                    {
                        RoleTag = spec.RoleTag,
                        UnitData = nData
                    });
                }
            }
        }

        return dummyMission;
    }
}