using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public class TileDataManager : MonoBehaviour, IInitializable
{
    private Dictionary<FloorType, TileDataSO> _floorLibrary = new();
    private Dictionary<PillarType, TileDataSO> _pillarLibrary = new();

    // [Refactoring Phase 1] 메모리 누수 방지용 핸들
    private AsyncOperationHandle<IList<TileDataSO>> _loadHandle;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            _floorLibrary.Clear();
            _pillarLibrary.Clear();

            _loadHandle = Addressables.LoadAssetsAsync<TileDataSO>("TileData", (so) =>
            {
                if (so == null) return;
                if (so.IsPillarData) { if (!_pillarLibrary.ContainsKey(so.PillarType)) _pillarLibrary.Add(so.PillarType, so); }
                else { if (!_floorLibrary.ContainsKey(so.FloorType)) _floorLibrary.Add(so.FloorType, so); }
            });

            await _loadHandle.ToUniTask();

            if (_floorLibrary.Count == 0) throw new InvalidOperationException("No Floor Data loaded!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TileDataManager] Error: {ex.Message}");
            throw;
        }
    }

    public TileDataSO GetFloorData(FloorType type) => _floorLibrary.TryGetValue(type, out var data) ? data : null;
    public TileDataSO GetPillarData(PillarType type) => _pillarLibrary.TryGetValue(type, out var data) ? data : null;
}