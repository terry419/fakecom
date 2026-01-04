using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CreateAssetMenu(fileName = "MapCatalog", menuName = "Data/Map/MapCatalog")]
public class MapCatalogSO : ScriptableObject
{
    [field: SerializeField]
    public List<MapPoolSO> DifficultyPools { get; private set; } = new List<MapPoolSO>();

    // [Change] MissionDifficulty -> float
    public bool TryGetPoolByDifficulty(float difficulty, out MapPoolSO pool)
    {
        // 1. 정확히 일치하는 난이도 찾기 (float 비교이므로 오차범위 고려 가능하지만, 보통 Editor 세팅값은 정확함)
        pool = DifficultyPools.FirstOrDefault(p => Mathf.Approximately(p.TargetDifficulty, difficulty));
        if (pool != null) return true;

        // 2. 없으면 가장 가까운 난이도 찾기
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

        HashSet<string> globalMissionNames = new HashSet<string>();
        // [Change] Set 타입을 float로 변경
        HashSet<float> difficultySet = new HashSet<float>();

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

            // [Change] float 중복 체크
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

            if (pool.Missions != null)
            {
                foreach (var mission in pool.Missions)
                {
                    if (mission == null) continue;

                    string mName = mission.Definition.MissionName;
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