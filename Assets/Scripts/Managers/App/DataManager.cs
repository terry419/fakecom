using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

// [의존성 명시] 초기화 순서: MapCatalogManager -> DataManager
[DependsOn(typeof(MapCatalogManager))]
public class DataManager : MonoBehaviour, IInitializable
{
    private MapCatalogManager _catalogManager;

    // [문제 2 해결] 메모리 관리를 위한 핸들 보관
    private AsyncOperationHandle<MapDataSO> _currentMapHandle;
    private bool _hasLoadedMap = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);
    private void OnDestroy()
    {
        // 앱 종료 시 정리
        UnloadMap();
        ServiceLocator.Unregister<DataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // Global Scope에서 이미 초기화된 CatalogManager를 가져옵니다.
        _catalogManager = ServiceLocator.Get<MapCatalogManager>();

        if (_catalogManager == null)
            throw new BootstrapException("[DataManager] Critical: MapCatalogManager missing. Dependency failed.");

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// MapID를 통해 무거운 MapDataSO를 비동기로 로드합니다.
    /// 기존에 로드된 맵이 있다면 자동으로 해제(Unload)합니다.
    /// </summary>
    public async UniTask<MapDataSO> LoadMapByIDAsync(string mapID)
    {
        // 1. 기존 맵 정리
        UnloadMap();

        // 2. 메타데이터(Entry) 조회
        if (!_catalogManager.TryGetEntryByID(mapID, out MapEntry entry))
        {
            throw new MapLoadException($"MapID '{mapID}' not found in Catalog.", mapID);
        }

        // 3. Addressable Key 유효성 재확인
        if (!entry.MapDataRef.RuntimeKeyIsValid())
        {
            throw new MapLoadException($"AssetReference key is invalid for MapID '{mapID}'.", mapID);
        }

        try
        {
            Debug.Log($"[DataManager] Loading Map Asset: {entry.DisplayName} ({mapID})...");

            // 4. 핸들을 보관하며 로드 (Release를 위해 필요)
            _currentMapHandle = entry.MapDataRef.LoadAssetAsync();
            var mapData = await _currentMapHandle.ToUniTask();

            if (mapData == null)
                throw new MapLoadException("Loaded asset is null.", mapID);

            // 로드 성공 표시
            _hasLoadedMap = true;

            return mapData;
        }
        catch (MapLoadException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new MapLoadException($"Addressable Load Failed: {ex.Message}", mapID, ex);
        }
    }

    /// <summary>
    /// 현재 로드된 맵 데이터를 메모리에서 해제합니다.
    /// </summary>
    public void UnloadMap()
    {
        if (_hasLoadedMap && _currentMapHandle.IsValid())
        {
            Debug.Log("[DataManager] Releasing current map asset...");
            Addressables.Release(_currentMapHandle);
        }

        _hasLoadedMap = false;
        // 핸들은 구조체이므로 null 처리 불필요, 상태만 리셋
    }
}