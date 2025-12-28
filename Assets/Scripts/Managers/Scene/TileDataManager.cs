using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class TileDataManager : MonoBehaviour, IInitializable
{
    // 바닥과 기둥 도서관 분리
    private Dictionary<FloorType, TileDataSO> _floorLibrary = new();
    private Dictionary<PillarType, TileDataSO> _pillarLibrary = new();

    public async UniTask Initialize(InitializationContext context)
    {
        Debug.Log("[TileDataManager] Initializing via Addressables...");
        _floorLibrary.Clear();
        _pillarLibrary.Clear();

        // "TileData" 라벨이 붙은 SO 로드
        var handle = Addressables.LoadAssetsAsync<TileDataSO>("TileData", (so) =>
        {
            if (so == null) return;

            if (so.IsPillarData)
            {
                if (so.PillarType != PillarType.None && !_pillarLibrary.ContainsKey(so.PillarType))
                    _pillarLibrary.Add(so.PillarType, so);
            }
            else
            {
                if (so.FloorType != FloorType.None && !_floorLibrary.ContainsKey(so.FloorType))
                    _floorLibrary.Add(so.FloorType, so);
            }
        });

        await handle.ToUniTask();
        Debug.Log($"[TileDataManager] Ready. Loaded {_floorLibrary.Count} floors, {_pillarLibrary.Count} pillars.");
    }

    public TileDataSO GetFloorData(FloorType type)
    {
        return _floorLibrary.TryGetValue(type, out var data) ? data : null;
    }

    public TileDataSO GetPillarData(PillarType type)
    {
        return _pillarLibrary.TryGetValue(type, out var data) ? data : null;
    }
}