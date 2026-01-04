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
        mission = Missions[Random.Range(0, Missions.Count)];
        return true;
    }

    // 중복 위치 제외 추출
    public bool TryGetRandomMissionExcluding(HashSet<string> excludedLocations, out MissionDataSO mission)
    {
        var candidates = Missions
            .Where(m => !excludedLocations.Contains(m.UI.LocationID))
            .ToList();

        if (candidates.Count == 0)
        {
            // 후보가 없으면 그냥 아무거나 중복 허용해서 리턴 (Fallback)
            return TryGetRandomMission(out mission);
        }

        mission = candidates[Random.Range(0, candidates.Count)];
        return true;
    }

    public bool Validate(out string error)
    {
        if (Missions.Any(m => m == null))
        {
            error = $"Pool {name} contains null missions.";
            return false;
        }
        error = string.Empty;
        return true;
    }
}