using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

[DependsOn(typeof(MapCatalogManager))]
public class DataManager : MonoBehaviour, IInitializable
{
    private AsyncOperationHandle<MapDataSO> _currentMapHandle;
    private bool _hasLoadedMap = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);
    private void OnDestroy()
    {
        UnloadMap();
        ServiceLocator.Unregister<DataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        var catalogMgr = ServiceLocator.Get<MapCatalogManager>();
        if (catalogMgr == null)
            throw new BootstrapException("[DataManager] MapCatalogManager dependency missing.");
        await UniTask.CompletedTask;
    }

    public async UniTask<MapDataSO> LoadMapFromMissionAsync(MissionDataSO mission)
    {
        if (mission == null) throw new ArgumentNullException(nameof(mission));

        UnloadMap();

        if (!mission.MapDataRef.RuntimeKeyIsValid())
        {
            throw new MapLoadException($"Invalid MapDataRef in mission '{mission.name}'", mission.name);
        }

        try
        {
            // [Fix] MissionSettings -> Definition
            Debug.Log($"[DataManager] Loading Map for Mission: {mission.Definition.MissionName}...");

            _currentMapHandle = mission.MapDataRef.LoadAssetAsync();
            var mapData = await _currentMapHandle.ToUniTask();

            if (mapData == null)
                throw new MapLoadException("Loaded map asset is null.", mission.name);

            _hasLoadedMap = true;
            return mapData;
        }
        catch (Exception ex)
        {
            throw new MapLoadException($"Map Load Failed: {ex.Message}", mission.name, ex);
        }
    }

    public void UnloadMap()
    {
        if (_hasLoadedMap && _currentMapHandle.IsValid())
        {
            Debug.Log("[DataManager] Releasing current map asset...");
            Addressables.Release(_currentMapHandle);
        }
        _hasLoadedMap = false;
    }
}