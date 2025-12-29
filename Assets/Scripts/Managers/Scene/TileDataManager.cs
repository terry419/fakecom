using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System;

public class TileDataManager : MonoBehaviour, IInitializable
{
    // [수정] id 문자열 대신, 기존 Enum을 키값으로 사용하는 딕셔너리 2개로 분리
    private Dictionary<FloorType, TileDataSO> _floorLibrary;
    private Dictionary<PillarType, TileDataSO> _pillarLibrary;

    // 메모리 해제 핸들
    private AsyncOperationHandle<IList<TileDataSO>> _loadHandle;
    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Global);
        // [리팩토링 3] 메모리 누수 방지
        if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        _floorLibrary = new Dictionary<FloorType, TileDataSO>();
        _pillarLibrary = new Dictionary<PillarType, TileDataSO>();

        try
        {
            // 라벨 "TileData"로 로드
            _loadHandle = Addressables.LoadAssetsAsync<TileDataSO>("TileData", null);
            IList<TileDataSO> results = await _loadHandle.ToUniTask();

            // [리팩토링 10] 로드 상태 검증
            if (_loadHandle.Status != AsyncOperationStatus.Succeeded || results == null)
            {
                throw new Exception("TileData 로드 실패 (Addressables Load Failed)");
            }

            foreach (var so in results)
            {
                if (so == null) continue;

                // [Fix] SO 파일을 건드리지 않고, 기존 필드(IsPillarData)로 분기 처리
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

            if (_floorLibrary.Count == 0 && _pillarLibrary.Count == 0)
            {
                Debug.LogWarning("[TileDataManager] 로드된 타일 데이터가 없습니다.");
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