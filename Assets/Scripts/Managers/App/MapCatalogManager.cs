using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

public class MapCatalogManager : MonoBehaviour, IInitializable
{
    private Dictionary<string, MapEntry> _idIndex;
    private Dictionary<string, List<MapEntry>> _tagIndex;

    private MapCatalogSO _catalog;
    private bool _isInitialized = false;

    // Global Scope 등록
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);
    private void OnDestroy() => ServiceLocator.Unregister<MapCatalogManager>(ManagerScope.Global);

    public async UniTask Initialize(InitializationContext context)
    {
        if (_isInitialized) return;

        // [수정] 직접 로드하지 않고 AppBootstrapper가 넘겨준 것을 사용합니다.
        // 기존 코드(Addressables.Load...)가 남아있다면 삭제해주세요.
        _catalog = context.MapCatalog;

        if (_catalog == null)
            throw new BootstrapException("[MapCatalogManager] Catalog not found in InitializationContext!");

        // 무결성 검사
        if (!_catalog.ValidateAllPools(out string err))
            throw new BootstrapException($"[MapCatalogManager] Validation Failed:\n{err}");

        // 인덱스 구축
        BuildIndices();

        _isInitialized = true;
        Debug.Log($"[MapCatalogManager] Initialized. Maps: {_idIndex.Count}, Tags: {_tagIndex.Count}");

        await UniTask.CompletedTask;
    }

    private void BuildIndices()
    {
        _idIndex = new Dictionary<string, MapEntry>();
        _tagIndex = new Dictionary<string, List<MapEntry>>();

        if (_catalog.DifficultyPools == null) return;

        foreach (var pool in _catalog.DifficultyPools)
        {
            if (pool == null || pool.Entries == null) continue;

            foreach (var entry in pool.Entries)
            {
                // 1. ID Indexing
                if (!string.IsNullOrEmpty(entry.MapID))
                {
                    _idIndex.TryAdd(entry.MapID, entry);
                }

                // 2. Tag Indexing
                if (entry.Tags != null)
                {
                    foreach (var rawTag in entry.Tags)
                    {
                        if (string.IsNullOrEmpty(rawTag)) continue;

                        string key = rawTag.ToLowerInvariant();

                        if (!_tagIndex.ContainsKey(key))
                            _tagIndex[key] = new List<MapEntry>();

                        // 중복 방지
                        if (!_tagIndex[key].Any(e => e.MapID == entry.MapID))
                        {
                            _tagIndex[key].Add(entry);
                        }
                    }
                }
            }
        }
    }

    // ========================================================================
    // Public API
    // ========================================================================

    public bool TryGetEntryByID(string mapID, out MapEntry entry)
    {
        if (!_isInitialized)
        {
            entry = default;
            return false;
        }
        return _idIndex.TryGetValue(mapID, out entry);
    }

    public bool TryGetRandomMapByDifficulty(int targetDifficulty, out MapEntry entry)
    {
        if (!_isInitialized || _catalog == null)
        {
            entry = default;
            return false;
        }

        if (_catalog.TryGetPoolByDifficulty(targetDifficulty, out var pool))
        {
            return pool.TryGetRandomEntry(out entry);
        }

        entry = default;
        return false;
    }

    public bool TryGetRandomMapByTag(string tag, out MapEntry entry)
    {
        string key = tag.ToLowerInvariant();
        if (_isInitialized && _tagIndex.TryGetValue(key, out var list) && list.Count > 0)
        {
            int index = UnityEngine.Random.Range(0, list.Count);
            entry = list[index];
            return true;
        }

        entry = default;
        return false;
    }
}