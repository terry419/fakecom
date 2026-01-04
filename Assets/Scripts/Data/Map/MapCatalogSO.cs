using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CreateAssetMenu(fileName = "MapCatalog", menuName = "Data/Map/MapCatalog")]
public class MapCatalogSO : ScriptableObject
{
    [field: SerializeField]
    public List<MapPoolSO> DifficultyPools { get; private set; } = new List<MapPoolSO>();

    // [Fix] 인자 타입 변경 (int -> MissionDifficulty)
    public bool TryGetPoolByDifficulty(MissionDifficulty difficulty, out MapPoolSO pool)
    {
        // Enum 직접 비교
        pool = DifficultyPools.FirstOrDefault(p => p.TargetDifficulty == difficulty);
        if (pool != null) return true;

        // [Fix] 근사치 찾기: Enum을 int로 변환하여 거리 계산
        pool = DifficultyPools.OrderBy(p => Mathf.Abs((int)p.TargetDifficulty - (int)difficulty)).FirstOrDefault();
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
        // [Fix] Set 타입 변경 (int -> MissionDifficulty)
        HashSet<MissionDifficulty> difficultySet = new HashSet<MissionDifficulty>();

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

            if (pool.Missions != null)
            {
                foreach (var mission in pool.Missions)
                {
                    if (mission == null) continue;

                    // [Fix] MissionSettings -> Definition 변경 반영
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