using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System;

public class TileDataManager : MonoBehaviour, IInitializable
{
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

        try
        {
            _loadHandle = Addressables.LoadAssetsAsync<TileDataSO>("TileData", null);
            IList<TileDataSO> results = await _loadHandle.ToUniTask();

            if (_loadHandle.Status != AsyncOperationStatus.Succeeded || results == null)
            {
                throw new Exception("TileData 로드 실패 (Addressables Load Failed)");
            }

            foreach (var so in results)
            {
                if (so == null) continue;

                if (so.IsPillarData)
                {
                    if (!_pillarLibrary.ContainsKey(so.PillarType))
                        _pillarLibrary.Add(so.PillarType, so);
                }
                else
                {
                    if (!_floorLibrary.ContainsKey(so.FloorType))
                        _floorLibrary.Add(so.FloorType, so);
                }
            }

            // [유지] 맵이 없는 초기 단계이므로, 데이터가 없어도 경고만 띄우고 넘어갑니다.
            if (_floorLibrary.Count == 0 && _pillarLibrary.Count == 0)
            {
                Debug.LogWarning("[TileDataManager] 로드된 타일 데이터가 없습니다. (초기 개발 단계 확인용)");
            }

            _isInitialized = true;
            Debug.Log($"[TileDataManager] Loaded: Floor({_floorLibrary.Count}), Pillar({_pillarLibrary.Count})");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TileDataManager] Initialize Failed: {ex.Message}");
            throw;
        }
    }

    public TileDataSO GetFloorData(FloorType type)
    {
        if (!_isInitialized) return null;
        return _floorLibrary.TryGetValue(type, out var data) ? data : null;
    }

    public TileDataSO GetPillarData(PillarType type)
    {
        if (!_isInitialized) return null;
        return _pillarLibrary.TryGetValue(type, out var data) ? data : null;
    }
}