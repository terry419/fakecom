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
        // DataManager는 이제 MapCatalogManager에 직접 의존하지 않고, 
        // 주어진 MissionData를 로드하는 유틸리티 성격이 강해졌습니다.
        // 하지만 [DependsOn]이 있으므로 형식상 가져옵니다.
        var catalogMgr = ServiceLocator.Get<MapCatalogManager>();
        if (catalogMgr == null)
            throw new BootstrapException("[DataManager] MapCatalogManager dependency missing.");

        await UniTask.CompletedTask;
    }

    // [Fix] ID 기반 로드 제거 -> MissionData 기반 로드로 변경
    // (MapEntry와 TryGetEntryByID가 사라졌으므로, 이 방식이 맞음)
    public async UniTask<MapDataSO> LoadMapFromMissionAsync(MissionDataSO mission)
    {
        if (mission == null) throw new ArgumentNullException(nameof(mission));

        // 1. 기존 맵 정리
        UnloadMap();

        // 2. Addressable Key 유효성 확인
        if (!mission.MapDataRef.RuntimeKeyIsValid())
        {
            throw new MapLoadException($"Invalid MapDataRef in mission '{mission.name}'", mission.name);
        }

        try
        {
            Debug.Log($"[DataManager] Loading Map for Mission: {mission.MissionSettings.MissionName}...");

            // 3. 핸들을 보관하며 로드
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