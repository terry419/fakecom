using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System;
using System.Linq;

public class TileDataManager : MonoBehaviour, IInitializable
{
    private MapEditorSettingsSO _visualSettings;
    private Dictionary<FloorType, TileDataSO> _floorLibrary;
    private Dictionary<PillarType, TileDataSO> _pillarLibrary;
    private AsyncOperationHandle<IList<TileDataSO>> _loadHandle;
    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Global);
        if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        _floorLibrary = new Dictionary<FloorType, TileDataSO>();
        _pillarLibrary = new Dictionary<PillarType, TileDataSO>();

        // [필수] 설정 주입
        _visualSettings = context.MapVisualSettings;

        // [Fail-Fast] 설정 누락 시 즉시 중단
        if (_visualSettings == null)
        {
            throw new BootstrapException(
                "[TileDataManager] CRITICAL: MapEditorSettingsSO missing in InitializationContext.\n" +
                "Check AppConfig -> MapVisualSettingsRef assignments.");
        }

        try
        {
            _loadHandle = Addressables.LoadAssetsAsync<TileDataSO>("TileData", null);
            IList<TileDataSO> results = await _loadHandle.ToUniTask();

            if (_loadHandle.Status == AsyncOperationStatus.Succeeded && results != null)
            {
                foreach (var so in results)
                {
                    if (so == null) continue;
                    if (so.IsPillarData)
                    {
                        if (!_pillarLibrary.ContainsKey(so.PillarType)) _pillarLibrary.Add(so.PillarType, so);
                    }
                    else
                    {
                        if (!_floorLibrary.ContainsKey(so.FloorType)) _floorLibrary.Add(so.FloorType, so);
                    }
                }
            }

            _isInitialized = true;
            Debug.Log($"[TileDataManager] Initialized. Floor Types: {_floorLibrary.Count}, Pillar Types: {_pillarLibrary.Count}");
        }
        catch (Exception ex)
        {
            // 상위 Bootstrapper에서 처리하도록 예외 전파
            throw new BootstrapException($"[TileDataManager] Logic Data Load Failed: {ex.Message}", ex);
        }
    }

    public GameObject GetFloorPrefab(FloorType type)
    {
        // 1. 초기화 여부 확인
        if (!_isInitialized)
        {
            throw new InvalidOperationException("[TileDataManager] Not initialized. EnsureGlobalSystems() might have failed.");
        }

        // 2. 설정 데이터 유효성 확인 (List<EditorFloorMapping> FloorMappings)
        if (_visualSettings == null || _visualSettings.FloorMappings == null || _visualSettings.FloorMappings.Count == 0)
        {
            throw new InvalidOperationException(
                "[TileDataManager] MapEditorSettingsSO has no FloorMappings configured.\n" +
                "Action: Add floor type mappings in the MapEditorSettings asset Inspector.");
        }

        // 3. 매핑 검색
        var mapping = _visualSettings.FloorMappings.FirstOrDefault(x => x.type == type);

        // 4. 특수 케이스: None/Void는 프리팹이 없을 수 있음 (정상)
        if (type == FloorType.None || type == FloorType.Void)
        {
            return mapping.prefab; // null 가능
        }

        // 5. 일반 타입인데 프리팹이 없는 경우 (데이터 누락)
        if (mapping.prefab == null)
        {
            // 현재 설정된 타입들 목록 생성 (디버깅용)
            var availableTypes = string.Join(", ",
                _visualSettings.FloorMappings
                    .Where(m => m.prefab != null)
                    .Select(m => m.type)
                    .Distinct());

            throw new KeyNotFoundException(
                $"[TileDataManager] Missing prefab for FloorType '{type}'.\n" +
                $"Available mapped types: [{availableTypes}]\n" +
                $"Action: Assign a prefab for '{type}' in MapEditorSettingsSO.");
        }

        return mapping.prefab;
    }

    public TileDataSO GetFloorData(FloorType type)
    {
        return _floorLibrary.TryGetValue(type, out var data) ? data : null;
    }
}