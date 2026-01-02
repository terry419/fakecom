using UnityEngine;
using System.Collections.Generic;

// [주의] MissionType, AIBehaviorType Enum 정의를 삭제했습니다.
// 이미 프로젝트 내 다른 파일에 정의되어 있으므로 그걸 그대로 가져다 씁니다.

[CreateAssetMenu(fileName = "NewMission", menuName = "Data/Mission/MissionData")]
public class MissionDataSO : ScriptableObject
{
    [Header("1. Basic Info")]
    public string MissionID;
    public string MissionName;
    [TextArea] public string Description;

    [Header("2. Map Reference")]
    [Tooltip("이 미션이 수행될 맵 데이터")]
    public MapDataSO MapData;

    // ------------------------------------------------------------------------
    // [아군 스폰]
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class AllySpawnData
    {
        [Tooltip("MapDataSO의 Tile에 설정된 RoleTag 이름 (예: Start_1)")]
        public string spawnTag;

        [Tooltip("스쿼드의 몇 번째 유닛을 배치할지 (0부터 시작)")]
        public int squadSlotIndex;

        [Tooltip("고정 유닛(NPC 등)이 필요하면 할당")]
        public UnitDataSO presetUnit;
    }

    [Header("3. Player Spawns")]
    public List<AllySpawnData> PlayerSpawns;

    // ------------------------------------------------------------------------
    // [적군 스폰]
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class EnemySpawnData
    {
        public UnitDataSO enemyUnit;

        [Tooltip("MapDataSO의 Tile에 설정된 RoleTag 이름 (예: Enemy_Sniper)")]
        public string spawnTag;

        public AIBehaviorType behavior = AIBehaviorType.Patrol; // 기존 Enum 사용
        public int lookDirection; // 0:North, 1:East, 2:South, 3:West
    }

    [Header("4. Enemy Spawns")]
    public List<EnemySpawnData> EnemySpawns;

    // ------------------------------------------------------------------------
    // [승리 조건]
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class VictoryCondition
    {
        public MissionType type; // 기존 Enum 사용
        public int surviveTurns;
        [Tooltip("목표 지점 태그 (예: Extract_Zone)")]
        public string targetLocationTag;
        public int requiredEliminateCount;
    }

    [Header("5. Victory Conditions")]
    public VictoryCondition victoryCondition;

    // ------------------------------------------------------------------------
    // [패배 조건]
    // ------------------------------------------------------------------------
    [System.Serializable]
    public class DefeatCondition
    {
        public enum DefeatType { AllAlliesDead, LeaderDead, TimeOut }
        public DefeatType type = DefeatType.AllAlliesDead;
        public int timeOutTurns = 15;
    }

    [Header("6. Defeat Conditions")]
    public DefeatCondition defeatCondition;

    [Header("7. Rewards")]
    public int GoldReward = 100;
}