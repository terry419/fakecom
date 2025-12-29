using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System;

public class EdgeDataManager : MonoBehaviour, IInitializable
{
    private Dictionary<EdgeDataType, EdgeDataSO> _edgeDataMap;
    private List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<EdgeDataManager>(ManagerScope.Global);
        foreach (var handle in _handles)
        {
            if (handle.IsValid()) Addressables.Release(handle);
        }
        _handles.Clear();
    }

    public async UniTask Initialize(InitializationContext context)
    {
        _edgeDataMap = new Dictionary<EdgeDataType, EdgeDataSO>();
        _handles.Clear();

        try
        {
            var loadHandle = Addressables.LoadAssetsAsync<EdgeDataSO>("EdgeData", null);
            _handles.Add(loadHandle);

            IList<EdgeDataSO> results = await loadHandle.ToUniTask();

            if (loadHandle.Status != AsyncOperationStatus.Succeeded || results == null)
            {
                throw new Exception("[EdgeDataManager] EdgeData 로드 실패.");
            }

            foreach (var edgeData in results)
            {
                if (edgeData.DataType == EdgeDataType.None) continue;

                if (_edgeDataMap.ContainsKey(edgeData.DataType))
                {
                    Debug.LogWarning($"[EdgeDataManager] 중복 데이터 무시됨: {edgeData.DataType}");
                    continue;
                }
                _edgeDataMap.Add(edgeData.DataType, edgeData);
            }

            // [1단계 적용됨] 필수 데이터 누락 검증 (Fail-Fast)
            ValidateRequiredData();

            _isInitialized = true;
            Debug.Log($"[EdgeDataManager] {_edgeDataMap.Count} Edge Data Loaded.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EdgeDataManager] Initialize Failed: {ex.Message}");
            throw;
        }
    }

    // [Fail-Fast] Enum에 정의된 모든 타입에 대한 데이터가 있는지 확인
    private void ValidateRequiredData()
    {
        var missingTypes = new List<string>();

        foreach (EdgeDataType type in Enum.GetValues(typeof(EdgeDataType)))
        {
            if (type == EdgeDataType.None) continue;
            if (!_edgeDataMap.ContainsKey(type))
            {
                missingTypes.Add(type.ToString());
            }
        }

        if (missingTypes.Count > 0)
        {
            string missingList = string.Join(", ", missingTypes);
            throw new InvalidOperationException($"[EdgeDataManager] Critical Error: 다음 재질(EdgeDataType)에 대한 데이터(SO)가 누락되었습니다 -> [{missingList}]");
        }
    }

    public EdgeInfo GetEdgeInfo(EdgeType type, EdgeDataType dataType)
    {
        if (!_isInitialized) return EdgeInfo.Open;

        EdgeDataSO data = null;
        if (_edgeDataMap.TryGetValue(dataType, out var foundData))
        {
            data = foundData;
        }
        else if (dataType != EdgeDataType.None)
        {
            Debug.LogWarning($"[EdgeDataManager] Unexpected missing data: {dataType}");
        }

        return EdgeInfo.Create(type, dataType, data);
    }
}