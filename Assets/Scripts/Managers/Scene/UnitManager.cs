using UnityEngine;
using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

[DependsOn(typeof(MapManager))]
public class UnitManager : MonoBehaviour, IInitializable
{
    private MapManager _mapManager;
    private List<Unit> _activeUnits = new List<Unit>();
    private Dictionary<string, GameObject> _prefabCache = new Dictionary<string, GameObject>();

    // [New] 초기화 완료 여부 플래그
    public bool IsInitialized { get; private set; } = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<UnitManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        _mapManager = ServiceLocator.Get<MapManager>();

        // [New] 초기화 완료 서명
        IsInitialized = true;

        await UniTask.CompletedTask;
    }

    public async UniTask<Unit> SpawnUnitAsync(UnitDataSO data, GridCoords coords)
    {
        // [Safety] 초기화되지 않았다면 예외 발생
        if (!IsInitialized || _mapManager == null)
            throw new InvalidOperationException("[UnitManager] Not initialized yet. Wait for SceneInitializer.");

        if (!_mapManager.HasTile(coords))
            throw new ArgumentException($"[UnitManager] Invalid Spawn Coords: {coords}");

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        GameObject prefab = await GetOrLoadPrefabAsync(data.ModelPrefab);

        GameObject go = Instantiate(prefab, transform);
        Unit unit = go.GetComponent<Unit>();
        if (unit == null) unit = go.AddComponent<Unit>();

        try
        {
            unit.Initialize(data);
            unit.SpawnOnMap(coords);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnitManager] Spawn Logic Failed: {ex.Message}");
            Destroy(go);
            throw;
        }

        _activeUnits.Add(unit);
        Debug.Log($"[UnitManager] Spawned {data.UnitName} at {coords}");

        return unit;
    }

    private async UniTask<GameObject> GetOrLoadPrefabAsync(AssetReferenceGameObject assetRef)
    {
        if (assetRef == null || !assetRef.RuntimeKeyIsValid())
            throw new InvalidOperationException($"[UnitManager] Invalid AssetReference.");

        string key = assetRef.RuntimeKey.ToString();

        if (_prefabCache.TryGetValue(key, out GameObject cachedPrefab))
        {
            if (cachedPrefab != null) return cachedPrefab;
            _prefabCache.Remove(key);
        }

        try
        {
            var loadedPrefab = await assetRef.LoadAssetAsync<GameObject>().ToUniTask();
            if (loadedPrefab == null) throw new InvalidOperationException($"Loaded asset is null: {key}");
            _prefabCache[key] = loadedPrefab;
            return loadedPrefab;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"[UnitManager] Failed to load prefab ({key}): {ex.Message}", ex);
        }
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit != null && _activeUnits.Contains(unit)) _activeUnits.Remove(unit);
    }

    public IReadOnlyList<Unit> GetAllUnits() => _activeUnits;
}