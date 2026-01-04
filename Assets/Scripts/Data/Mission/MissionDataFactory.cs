using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerSpawnerSpec
{
    public string UnitName;
    public int HP;
}

[System.Serializable]
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

[System.Serializable]
public struct MissionDataFactoryConfig
{
    public int MaxPlayerCount;
    public string PlayerSpawnTag;
    public bool AddDummyEnemies;
    public List<EnemySpawnerSpec> DummyEnemies;
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
        dummyMission.hideFlags = HideFlags.DontSave;

        dummyMission.MapDataRef = mapEntry.MapDataRef;
        dummyMission.MissionSettings = new MissionSettings
        {
            MissionName = mapEntry.MapID,
            Type = MissionType.Exterminate
        };

        // 1. Player Config
        dummyMission.PlayerConfig = new PlayerMissionConfig();
        for (int i = 0; i < cfg.MaxPlayerCount; i++)
        {
            dummyMission.PlayerConfig.SpawnSlots.Add(new PlayerSpawnSlot { RoleTag = cfg.PlayerSpawnTag });
        }

        if (cfg.AddDummyPlayers && cfg.DummyPlayers != null)
        {
            foreach (var spec in cfg.DummyPlayers)
            {
                var pData = ScriptableObject.CreateInstance<PlayerUnitDataSO>();
                pData.hideFlags = HideFlags.DontSave;
                pData.name = spec.UnitName;
                pData.UnitName = spec.UnitName;
                pData.MaxHP = spec.HP;
                pData.Mobility = 6;
                pData.Aim = 75;
                dummyMission.PlayerConfig.DefaultSquad.Add(pData);
            }
        }

        // 2. Enemy Spawns
        dummyMission.EnemySpawns = new List<EnemySpawnDef>();
        if (cfg.AddDummyEnemies && cfg.DummyEnemies != null)
        {
            foreach (var spec in cfg.DummyEnemies)
            {
                for (int i = 0; i < spec.Count; i++)
                {
                    var eData = ScriptableObject.CreateInstance<EnemyUnitDataSO>();
                    eData.hideFlags = HideFlags.DontSave;
                    eData.name = $"{spec.NamePrefix}_{i}";
                    eData.UnitName = spec.NamePrefix;
                    eData.MaxHP = spec.HP;
                    eData.Mobility = 5;
                    eData.Aim = 60;

                    dummyMission.EnemySpawns.Add(new EnemySpawnDef
                    {
                        RoleTag = spec.RoleTag,
                        UnitData = eData
                    });
                }
            }
        }

        // 3. Neutral Spawns
        dummyMission.NeutralSpawns = new List<NeutralSpawnDef>();
        if (cfg.AddDummyNeutrals && cfg.DummyNeutrals != null)
        {
            foreach (var spec in cfg.DummyNeutrals)
            {
                for (int i = 0; i < spec.Count; i++)
                {
                    var nData = ScriptableObject.CreateInstance<EnemyUnitDataSO>();
                    nData.hideFlags = HideFlags.DontSave;
                    nData.name = $"Neutral_{spec.NamePrefix}_{i}";
                    nData.UnitName = spec.NamePrefix;
                    nData.MaxHP = spec.HP;
                    nData.Mobility = 0;

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