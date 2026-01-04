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

        // AppBootstrapper가 로드해서 넘겨준 CatalogSO를 저장
        _catalog = context.MapCatalog;

        if (_catalog == null)
            throw new BootstrapException("[MapCatalogManager] Catalog not found in Context!");

        // 유효성 검사
        if (!_catalog.ValidateAllPools(out string err))
            throw new BootstrapException($"[MapCatalogManager] Validation Failed:\n{err}");

        _isInitialized = true;
        Debug.Log($"[MapCatalogManager] Initialized. Difficulty Pools: {_catalog.DifficultyPools.Count}");
        await UniTask.CompletedTask;
    }

    // ========================================================================
    // Public API
    // ========================================================================

    // [Fix] BootManager에서 참조할 수 있도록 Getter 제공
    public MapCatalogSO GetCatalogSO() => _catalog;

    // [Fix] MapEntry 대신 MissionDataSO를 반환하도록 변경
    public bool TryGetRandomMissionByDifficulty(int targetDifficulty, out MissionDataSO mission)
    {
        if (!_isInitialized || _catalog == null)
        {
            mission = null;
            return false;
        }

        // 1. 해당 난이도의 풀을 찾는다
        if (_catalog.TryGetPoolByDifficulty(targetDifficulty, out var pool))
        {
            // 2. 풀에서 미션을 랜덤하게 뽑는다 (MapPoolSO에 새로 만든 메서드 사용)
            return pool.TryGetRandomMission(out mission);
        }

        mission = null;
        return false;
    }
}