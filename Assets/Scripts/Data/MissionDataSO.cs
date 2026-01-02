using UnityEngine;
using System.Collections.Generic;

namespace YCOM.Mission // [Fix] CS0101 방지용 네임스페이스
{
    // 열거형도 네임스페이스 안으로 이동
    public enum MissionType { EliminateAll, ReachTarget, SurviveTurns }
    public enum AIBehaviorType { Patrol, Guard, Aggressive }

    [CreateAssetMenu(fileName = "NewMission", menuName = "YCOM/Data/MissionData")]
    public class MissionDataSO : ScriptableObject
    {
        [Header("1. Basic Info")]
        public string MissionID;
        public string MissionName;
        [TextArea] public string Description;

        [Header("2. Map Reference")]
        public MapDataSO MapData;

        // ------------------------------------------------------------------------
        // [수정] GridCoords(좌표) -> string(태그)로 변경
        // ------------------------------------------------------------------------
        [System.Serializable]
        public class AllySpawnData
        {
            [Tooltip("MapDataSO에 정의된 태그 이름 (예: Player_Start_1)")]
            public string spawnTag;

            public int squadSlotIndex;
            public UnitDataSO presetUnit;
        }

        [Header("3. Player Spawns")]
        public List<AllySpawnData> PlayerSpawns;

        // ------------------------------------------------------------------------

        [System.Serializable]
        public class EnemySpawnData
        {
            public UnitDataSO enemyUnit;

            [Tooltip("MapDataSO에 정의된 태그 이름 (예: Enemy_Patrol_A)")]
            public string spawnTag;

            public AIBehaviorType behavior = AIBehaviorType.Patrol;
            public int lookDirection;
        }

        [Header("4. Enemy Spawns")]
        public List<EnemySpawnData> EnemySpawns;

        // ------------------------------------------------------------------------
        // 승리/패배/보상 조건 (기존 유지)
        // ------------------------------------------------------------------------
        [System.Serializable]
        public class VictoryCondition
        {
            public MissionType type;
            public int surviveTurns;

            [Tooltip("목표 지점 태그 (예: Extract_Point)")]
            public string targetLocationTag; // 여기도 좌표 대신 태그!

            public int requiredEliminateCount;
        }
        [Header("5. Victory Conditions")]
        public VictoryCondition victoryCondition;

        [System.Serializable]
        public class DefeatCondition
        {
            public enum DefeatType { AllAlliesDead, LeaderDead, TimeOut }
            public DefeatType type = DefeatType.AllAlliesDead;
            public int timeOutTurns = 15;
        }
        [Header("6. Defeat Conditions")]
        public DefeatCondition defeatCondition;

        [System.Serializable]
        public class MissionReward
        {
            public int gold = 100;
            public int experience = 50;
        }
        [Header("7. Rewards")]
        public MissionReward reward;
    }
}