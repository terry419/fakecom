using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class MapCatalogManager : MonoBehaviour, IInitializable
{
    private MapCatalogSO _catalog;
    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);
    private void OnDestroy() => ServiceLocator.Unregister<MapCatalogManager>(ManagerScope.Global);

    public async UniTask Initialize(InitializationContext context)
    {
        if (_isInitialized) return;
        _catalog = context.MapCatalog;

        if (_catalog == null)
            throw new BootstrapException("[MapCatalogManager] Catalog not found in Context!");

        if (!_catalog.ValidateAllPools(out string err))
            throw new BootstrapException($"[MapCatalogManager] Validation Failed:\n{err}");

        _isInitialized = true;
        Debug.Log($"[MapCatalogManager] Initialized. Difficulty Pools: {_catalog.DifficultyPools.Count}");
        await UniTask.CompletedTask;
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public MapCatalogSO GetCatalogSO() => _catalog;

    // [Refactor] 기존 메서드 업데이트 (int -> MissionDifficulty)
    public bool TryGetRandomMissionByDifficulty(int targetDifficulty, out MissionDataSO mission)
    {
        if (!_isInitialized || _catalog == null)
        {
            mission = null;
            return false;
        }

        // 1. 정확히 일치하는 난이도 찾기
        var pool = _catalog.DifficultyPools.FirstOrDefault(p => p.TargetDifficulty == targetDifficulty);

        // 2. 없으면 가장 가까운 난이도 찾기 (int니까 절대값 비교 가능)
        if (pool == null)
        {
            pool = _catalog.DifficultyPools.OrderBy(p => Mathf.Abs(p.TargetDifficulty - targetDifficulty)).FirstOrDefault();
        }

        if (pool != null)
        {
            return pool.TryGetRandomMission(out mission);
        }

        mission = null;
        return false;
    }

    // [Revert] int difficulty 사용
    public List<MissionDataSO> GetDistinctMissions(int difficulty, int count)
    {
        var results = new List<MissionDataSO>();
        var usedLocations = new HashSet<string>();

        if (!_isInitialized || _catalog == null) return results;

        // 정확한 난이도 풀 찾기 (없으면 근사치)
        var pool = _catalog.DifficultyPools.FirstOrDefault(p => p.TargetDifficulty == difficulty);
        if (pool == null)
        {
            pool = _catalog.DifficultyPools.OrderBy(p => Mathf.Abs(p.TargetDifficulty - difficulty)).FirstOrDefault();
        }

        if (pool == null)
        {
            Debug.LogWarning($"[MapCatalogManager] No pool found for Difficulty {difficulty}");
            return results;
        }

        for (int i = 0; i < count; i++)
        {
            if (pool.TryGetRandomMissionExcluding(usedLocations, out var mission))
            {
                results.Add(mission);
                if (!string.IsNullOrEmpty(mission.UI.LocationID))
                {
                    usedLocations.Add(mission.UI.LocationID);
                }
            }
        }
        return results;
    }
}