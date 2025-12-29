using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets; // 필수
using UnityEngine.ResourceManagement.AsyncOperations; // 필수
using System;

public class EdgeDataManager : MonoBehaviour, IInitializable
{
    // 데이터 저장소
    private Dictionary<EdgeDataType, EdgeDataSO> _library = new();
    private AsyncOperationHandle<IList<EdgeDataSO>> _loadHandle;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        if (_loadHandle.IsValid()) Addressables.Release(_loadHandle);
        ServiceLocator.Unregister<EdgeDataManager>(ManagerScope.Global);
    }
    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            _library.Clear();
            _loadHandle = Addressables.LoadAssetsAsync<EdgeDataSO>("EdgeData", (so) =>
            {
                if (so != null && so.DataType != EdgeDataType.None && !_library.ContainsKey(so.DataType))
                    _library.Add(so.DataType, so);
            });
            await _loadHandle.ToUniTask();

            // 검증 로직
            foreach (EdgeDataType type in Enum.GetValues(typeof(EdgeDataType)))
            {
                if (type != EdgeDataType.None && !_library.ContainsKey(type))
                    throw new InvalidOperationException($"Missing EdgeData for: {type}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EdgeDataManager] Error: {ex.Message}");
            throw;
        }
    }

    private void ValidateAllDataLoaded()
    {
        foreach (EdgeDataType type in System.Enum.GetValues(typeof(EdgeDataType)))
        {
            if (type == EdgeDataType.None) continue;

            if (!_library.ContainsKey(type))
            {
                Debug.LogError($"[EdgeDataManager] MISSING DATA: '{type}' 타입의 데이터가 로드되지 않았습니다. SO 파일에 'EdgeData' 라벨을 붙였는지 확인하세요.");
            }
        }
    }

    public EdgeDataSO GetData(EdgeDataType type) => _library.TryGetValue(type, out var data) ? data : null;
}