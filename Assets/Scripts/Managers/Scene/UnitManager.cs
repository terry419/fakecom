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
    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    private MissionDataSO _currentMission;
    public bool IsInitialized { get; private set; } = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<UnitManager>(ManagerScope.Scene);

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
            Debug.LogWarning("[UnitManager] No MissionData. Auto-spawn skipped.");
        }

        IsInitialized = true;
    }

    private async UniTask SpawnMissionUnitsAsync()
    {
        Debug.Log("[UnitManager] Starting Auto-Spawn...");
        HashSet<GridCoords> usedTiles = new HashSet<GridCoords>();

        // 1. Player
        if (_currentMission.PlayerConfig != null)
        {
            var squad = _currentMission.PlayerConfig.DefaultSquad;
            var slots = _currentMission.PlayerConfig.SpawnSlots;
            int count = Mathf.Min(squad.Count, slots.Count);

            for (int i = 0; i < count; i++)
            {
                string tag = slots[i].RoleTag;
                if (TryGetAvailableTile(tag, usedTiles, out var tile))
                {
                    await SpawnUnitAsync(squad[i], tile.Coords, Faction.Player);
                }
                else
                {
                    Debug.LogWarning($"[UnitManager] Spawn Skipped: Player '{squad[i].name}' (Tag={tag} full/missing)");
                }
            }
        }

        // 2. Enemy
        if (_currentMission.EnemySpawns != null)
        {
            await SpawnFactionUnits(_currentMission.EnemySpawns, d => d.RoleTag, d => d.UnitData, Faction.Enemy, usedTiles);
        }

        // 3. Neutral
        if (_currentMission.NeutralSpawns != null)
        {
            await SpawnFactionUnits(_currentMission.NeutralSpawns, d => d.RoleTag, d => d.UnitData, Faction.Neutral, usedTiles);
        }

        Debug.Log($"[UnitManager] Spawn Complete. Units: {_activeUnits.Count}");
    }

    private async UniTask SpawnFactionUnits<T>(
        IEnumerable<T> spawnDefs,
        Func<T, string> tagSelector,
        Func<T, UnitDataSO> dataSelector,
        Faction faction,
        HashSet<GridCoords> usedTiles)
    {
        if (spawnDefs == null) return;
        foreach (var def in spawnDefs)
        {
            string tag = tagSelector(def);
            UnitDataSO data = dataSelector(def);

            if (TryGetAvailableTile(tag, usedTiles, out var tile))
            {
                await SpawnUnitAsync(data, tile.Coords, faction);
            }
            else
            {
                Debug.LogWarning($"[UnitManager] Spawn Skipped: {faction} Unit '{data.name}' (Tag={tag} full/missing)");
            }
        }
    }

    private bool TryGetAvailableTile(string tag, HashSet<GridCoords> usedTiles, out TileData tile)
    {
        int maxAttempts = 10;
        for (int i = 0; i < maxAttempts; i++)
        {
            if (_mapManager.TryGetRandomTileByTag(tag, out tile))
            {
                if (!usedTiles.Contains(tile.Coords) && !_mapManager.HasUnit(tile.Coords))
                {
                    usedTiles.Add(tile.Coords);
                    return true;
                }
            }
        }
        tile = default;
        return false;
    }

    public async UniTask<Unit> SpawnUnitAsync(UnitDataSO data, GridCoords coords, Faction faction)
    {
        if (_mapManager == null) return null;

        GameObject prefab = await GetOrLoadPrefabAsync(data.ModelPrefab);
        GameObject go = Instantiate(prefab, _mapManager.GridToWorld(coords), Quaternion.identity);

        Unit unit = go.GetComponent<Unit>();
        if (unit == null) unit = go.AddComponent<Unit>();

        try
        {
            unit.Initialize(data, faction);
            _activeUnits.Add(unit);
            _mapManager.RegisterUnit(coords, unit);
            unit.SpawnOnMap(coords);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnitManager] Spawn Error: {ex.Message}");
            Destroy(go);
            throw;
        }

        if (faction == Faction.Player)
        {
            if (_cachedPlayerController == null) _cachedPlayerController = FindObjectOfType<PlayerController>();
            if (_cachedPlayerController != null && _cachedPlayerController.PossessedUnit == null)
            {
                _cachedPlayerController.Possess(unit);
            }
        }

        return unit;
    }

    private async UniTask<GameObject> GetOrLoadPrefabAsync(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
            return await Addressables.LoadAssetAsync<GameObject>("BasicUnit").ToUniTask();

        string key = assetRef.RuntimeKey.ToString();
        if (_prefabCache.TryGetValue(key, out GameObject cached) && cached != null) return cached;

        var loaded = await assetRef.LoadAssetAsync<GameObject>().ToUniTask();
        _prefabCache[key] = loaded;
        return loaded;
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit != null && _activeUnits.Contains(unit)) _activeUnits.Remove(unit);
    }

    public IReadOnlyList<Unit> GetAllUnits() => _activeUnits;
}