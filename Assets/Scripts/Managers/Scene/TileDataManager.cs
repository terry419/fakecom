using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System;
using System.Linq; // 리스트 검색용

public class TileDataManager : MonoBehaviour, IInitializable
{
    // [핵심 변경] 진짜 프리팹이 들어있는 설정 파일 참조
    [Header("Visual Settings (Source of Truth)")]
    [SerializeField] private MapEditorSettingsSO _visualSettings;

    // 논리 데이터 (기존 유지)
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

        // [방어 코드] 시각 설정 파일이 연결 안 되어 있으면 경고
        if (_visualSettings == null)
        {
            Debug.LogError("[TileDataManager] CRITICAL: 'MapEditorSettingsSO' is NOT assigned in Inspector! Map will be invisible.");
        }

        try
        {
            // 논리 데이터 로딩 (기존 유지 - 소리, 스탯 등)
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
            Debug.Log($"[TileDataManager] Initialized. Logic Data: {_floorLibrary.Count}, Visual Source: {(_visualSettings != null ? "Linked" : "Missing")}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TileDataManager] Initialize Failed: {ex.Message}");
            throw;
        }
    }

    // [핵심 변경] TileDataSO가 아니라 MapEditorSettingsSO에서 프리팹을 찾습니다.
    public GameObject GetFloorPrefab(FloorType type)
    {
        if (_visualSettings == null) return null;

        // MapEditorSettingsSO의 리스트를 뒤져서 ID(type)가 같은 놈의 Prefab을 리턴
        var mapping = _visualSettings.FloorMappings.FirstOrDefault(x => x.type == type);

        // 구조체(struct)라 null 체크 대신 프리팹 유무 확인
        if (mapping.prefab != null)
        {
            return mapping.prefab;
        }

        Debug.LogWarning($"[TileDataManager] No Prefab found in MapEditorSettings for: {type}");
        return null;
    }

    // (필요 시 기둥도 동일한 방식으로 추가 가능)
    public TileDataSO GetFloorData(FloorType type)
    {
        return _floorLibrary.TryGetValue(type, out var data) ? data : null;
    }
}