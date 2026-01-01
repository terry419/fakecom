using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

[CreateAssetMenu(fileName = "MapCatalog", menuName = "Data/Map/MapCatalog")]
public class MapCatalogSO : ScriptableObject
{
    [field: SerializeField]
    public List<MapPoolSO> DifficultyPools { get; private set; } = new List<MapPoolSO>();

    // [개선 2] TryGet 패턴 적용
    public bool TryGetPoolByDifficulty(int difficulty, out MapPoolSO pool)
    {
        // 정확히 일치하는 난이도 우선
        pool = DifficultyPools.FirstOrDefault(p => p.TargetDifficulty == difficulty);
        if (pool != null) return true;

        // 없으면 근사치 (Fallback) - 로직상 필요하다면 유지
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

    // [개선 3] out parameter 추가
    public bool ValidateAllPools(out string errorMsg)
    {
        StringBuilder sb = new StringBuilder();
        bool isTotalValid = true;

        HashSet<string> globalIdSet = new HashSet<string>();
        // [개선 7] 난이도 중복 검사용 Set
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

            // 1. 난이도 중복 검사
            if (difficultySet.Contains(pool.TargetDifficulty))
            {
                sb.AppendLine($" - Error: Duplicate Difficulty Tier {pool.TargetDifficulty} in pool '{pool.name}'");
                isTotalValid = false;
            }
            else
            {
                difficultySet.Add(pool.TargetDifficulty);
            }

            // 2. 풀 자체 검증 (Fail-Fast 메시지 수신)
            if (!pool.Validate(out string poolErr))
            {
                sb.AppendLine(poolErr);
                isTotalValid = false;
            }

            // 3. 글로벌 ID 중복 검사
            if (pool.Entries != null)
            {
                foreach (var entry in pool.Entries)
                {
                    if (string.IsNullOrEmpty(entry.MapID)) continue;

                    if (globalIdSet.Contains(entry.MapID))
                    {
                        sb.AppendLine($" - GLOBAL DUPLICATE ID: '{entry.MapID}' (Last checked: {pool.name})");
                        isTotalValid = false;
                    }
                    else
                    {
                        globalIdSet.Add(entry.MapID);
                    }
                }
            }
        }

        errorMsg = sb.ToString();
        return isTotalValid;
    }
}