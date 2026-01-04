using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CreateAssetMenu(fileName = "MapCatalog", menuName = "Data/Map/MapCatalog")]
public class MapCatalogSO : ScriptableObject
{
    [field: SerializeField]
    public List<MapPoolSO> DifficultyPools { get; private set; } = new List<MapPoolSO>();

    public bool TryGetPoolByDifficulty(int difficulty, out MapPoolSO pool)
    {
        pool = DifficultyPools.FirstOrDefault(p => p.TargetDifficulty == difficulty);
        if (pool != null) return true;

        pool = DifficultyPools.OrderBy(p => Mathf.Abs(p.TargetDifficulty - difficulty)).FirstOrDefault();
        return pool != null;
    }

    [ContextMenu("Validate Catalog")]
    public void ValidateCatalogContext()
    {
        if (ValidateAllPools(out string error))
        {
            Debug.Log("<color=green>[MapCatalog] Validation Success!</color>");
        }
        else
        {
            Debug.LogError($"<color=red>[MapCatalog] Validation Failed:</color>\n{error}");
        }
    }

    public bool ValidateAllPools(out string errorMsg)
    {
        StringBuilder sb = new StringBuilder();
        bool isTotalValid = true;

        // [Fix] Mission 이름 중복 검사로 변경
        HashSet<string> globalMissionNames = new HashSet<string>();
        HashSet<int> difficultySet = new HashSet<int>();

        if (DifficultyPools == null || DifficultyPools.Count == 0)
        {
            errorMsg = "Catalog has no pools linked.";
            return false;
        }

        foreach (var pool in DifficultyPools)
        {
            if (pool == null)
            {
                sb.AppendLine(" - Error: Found null pool reference.");
                isTotalValid = false;
                continue;
            }

            if (difficultySet.Contains(pool.TargetDifficulty))
            {
                sb.AppendLine($" - Error: Duplicate Difficulty Tier {pool.TargetDifficulty} in pool '{pool.name}'");
                isTotalValid = false;
            }
            else
            {
                difficultySet.Add(pool.TargetDifficulty);
            }

            if (!pool.Validate(out string poolErr))
            {
                sb.AppendLine(poolErr);
                isTotalValid = false;
            }

            // [Fix] Entries -> Missions 변경 대응
            if (pool.Missions != null)
            {
                foreach (var mission in pool.Missions)
                {
                    if (mission == null) continue;

                    // MissionName으로 중복 검사
                    string mName = mission.MissionSettings.MissionName;
                    if (string.IsNullOrEmpty(mName)) continue;

                    if (globalMissionNames.Contains(mName))
                    {
                        sb.AppendLine($" - GLOBAL DUPLICATE MISSION: '{mName}' (Last checked: {pool.name})");
                        isTotalValid = false;
                    }
                    else
                    {
                        globalMissionNames.Add(mName);
                    }
                }
            }
        }

        errorMsg = sb.ToString();
        return isTotalValid;
    }
}