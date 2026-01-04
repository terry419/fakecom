using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewMapPool", menuName = "Data/Map/MapPool")]
public class MapPoolSO : ScriptableObject
{
    [field: Header("Pool Settings")]
    [field: SerializeField]
    [field: Range(1f, 10f)]
    public float TargetDifficulty { get; private set; } = 1f;
    [Header("Missions")]
    public List<MissionDataSO> Missions = new List<MissionDataSO>();

    public bool TryGetRandomMission(out MissionDataSO mission)
    {
        if (Missions == null || Missions.Count == 0)
        {
            mission = null;
            return false;
        }
        int index = UnityEngine.Random.Range(0, Missions.Count);
        mission = Missions[index];
        return mission != null;
    }

    // 중복 위치 제외 추출
    public bool TryGetRandomMissionExcluding(HashSet<string> excludedLocationIDs, out MissionDataSO mission)
    {
        mission = null;
        if (Missions == null || Missions.Count == 0) return false;

        var candidates = Missions.Where(m =>
            m != null &&
            (string.IsNullOrEmpty(m.UI.LocationID) || !excludedLocationIDs.Contains(m.UI.LocationID))
        ).ToList();

        if (candidates.Count == 0) return false;

        mission = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }

    public bool Validate(out string errorMsg)
    {
        if (Missions == null || Missions.Count == 0)
        {
            errorMsg = $"[Pool {name}] has no missions!";
            return false;
        }
        // ... (나머지 검증 로직은 기존 유지) ...
        errorMsg = string.Empty;
        return true;
    }
}