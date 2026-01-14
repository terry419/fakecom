using UnityEngine;
using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

[DependsOn(typeof(MapManager))]
public class UnitManager : MonoBehaviour, IInitializable
{
    private MapManager _mapManager;
    private PlayerController _cachedPlayerController;

    private List<Unit> _activeUnits = new List<Unit>();

    // [Fix] 리소스 해제를 위해 핸들 관리가 필요하지만, 
    // 여기서는 간편하게 GameObject 자체를 Release 하기 위해 캐시 유지
    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    private MissionDataSO _currentMission;
    public bool IsInitialized { get; private set; } = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);

    private void OnDestroy()
    {
        // 1. 서비스 등록 해제
        ServiceLocator.Unregister<UnitManager>(ManagerScope.Scene);

        // 2. [Fix] Addressable 리소스 해제 (메모리 누수 방지)
        foreach (var prefab in _prefabCache.Values)
        {
            if (prefab != null)
            {
                Addressables.Release(prefab);
            }
        }
        _prefabCache.Clear();
        _activeUnits.Clear();
    }

    public async UniTask Initialize(InitializationContext context)
    {
        _mapManager = ServiceLocator.Get<MapManager>();
        _cachedPlayerController = FindObjectOfType<PlayerController>();
        _currentMission = context.MissionData;

        if (_currentMission != null)
        {
            await SpawnMissionUnitsAsync();
        }
        else
        {
            Debug.LogWarning("[UnitManager] No MissionData provided.");
        }

        IsInitialized = true;
    }

    private async UniTask SpawnMissionUnitsAsync()
    {
        HashSet<GridCoords> usedTiles = new HashSet<GridCoords>();

        // 1. Player Spawn
        if (_currentMission.PlayerConfig != null)
        {
            var squad = _currentMission.PlayerConfig.DefaultSquad;
            var slots = _currentMission.PlayerConfig.SpawnSlots;
            int count = Mathf.Min(squad.Count, slots.Count);

            for (int i = 0; i < count; i++)
            {
                string tag = slots[i].RoleTag;
                if (TryGetAvailableTile(tag, usedTiles, out Tile tile))
                {
                    await SpawnUnitAsync(squad[i], tile.Coordinate, Faction.Player);
                }
                else
                {
                    Debug.LogWarning($"[UnitManager] Spawn Skipped: Player '{squad[i].name}' (Tag={tag} missing/blocked)");
                }
            }
        }

        // 2. Enemy Spawn
        if (_currentMission.EnemySpawns != null)
            await SpawnFactionUnits(_currentMission.EnemySpawns, d => d.RoleTag, d => d.UnitData, Faction.Enemy, usedTiles);

        // 3. Neutral Spawn
        if (_currentMission.NeutralSpawns != null)
            await SpawnFactionUnits(_currentMission.NeutralSpawns, d => d.RoleTag, d => d.UnitData, Faction.Neutral, usedTiles);
    }

    private async UniTask SpawnFactionUnits<T>(IEnumerable<T> spawnDefs, Func<T, string> tagSelector, Func<T, UnitDataSO> dataSelector, Faction faction, HashSet<GridCoords> usedTiles)
    {
        if (spawnDefs == null) return;
        foreach (var def in spawnDefs)
        {
            string tag = tagSelector(def);
            UnitDataSO data = dataSelector(def);

            if (TryGetAvailableTile(tag, usedTiles, out Tile tile))
            {
                await SpawnUnitAsync(data, tile.Coordinate, faction);
            }
            else
            {
                Debug.LogWarning($"[UnitManager] Spawn Skipped: {faction} '{data.name}' (Tag={tag} missing/blocked)");
            }
        }
    }

    private bool TryGetAvailableTile(string tag, HashSet<GridCoords> usedTiles, out Tile tile)
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            if (_mapManager.TryGetRandomTileByTag(tag, out tile))
            {
                if (!usedTiles.Contains(tile.Coordinate))
                {
                    usedTiles.Add(tile.Coordinate);
                    return true;
                }
            }
        }
        tile = null;
        return false;
    }

    public async UniTask<Unit> SpawnUnitAsync(UnitDataSO data, GridCoords coords, Faction faction)
    {
        if (_mapManager == null)
            throw new InvalidOperationException("[UnitManager] MapManager is null.");

        if (data == null)
            throw new ArgumentNullException(nameof(data), "[UnitManager] Cannot spawn unit with null Data.");

        GameObject prefab = await GetOrLoadPrefabAsync(data.ModelPrefab);

        GameObject go = Instantiate(prefab, _mapManager.GridToWorld(coords), Quaternion.identity);
        go.name = data.UnitName;

        Unit unit = go.GetComponent<Unit>();
        if (unit == null) unit = go.AddComponent<Unit>();

        try
        {
            // [Refactor] 책임 분리: Unit.Initialize가 Map 등록까지 전담
            // 1. 초기화 호출 (내부에서 SpawnOnMap -> MapManager.RegisterUnit 수행)
            unit.Initialize(coords, data, faction);

            // 2. 관리 목록에만 추가
            _activeUnits.Add(unit);

            // [삭제됨] 중복 등록 방지
            // _mapManager.RegisterUnit(coords, unit);
            // unit.SpawnOnMap();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnitManager] Spawn Logic Error: {ex.Message}");
            Destroy(go);
            throw;
        }

        // 플레이어 유닛 자동 빙의 (Possess)
        if (faction == Faction.Player)
        {
            if (_cachedPlayerController == null) _cachedPlayerController = FindObjectOfType<PlayerController>();
            if (_cachedPlayerController != null && _cachedPlayerController.PossessedUnit == null)
            {
                await _cachedPlayerController.Possess(unit);
            }
        }

        return unit;
    }

    private async UniTask<GameObject> GetOrLoadPrefabAsync(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
        {
            throw new InvalidOperationException($"[UnitManager] Invalid Prefab Reference in UnitData.");
        }

        string key = assetRef.RuntimeKey.ToString();

        if (_prefabCache.TryGetValue(key, out GameObject cached) && cached != null)
            return cached;

        try
        {
            var loaded = await assetRef.LoadAssetAsync<GameObject>().ToUniTask();
            if (loaded == null)
            {
                throw new InvalidOperationException($"[UnitManager] Failed to load prefab: {key}");
            }
            _prefabCache[key] = loaded;
            return loaded;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[UnitManager] Addressable Load Failed for '{key}': {ex.Message}", ex);
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit != null && _activeUnits.Contains(unit)) _activeUnits.Remove(unit);
    }

    public IReadOnlyList<Unit> GetAllUnits() => _activeUnits;
}