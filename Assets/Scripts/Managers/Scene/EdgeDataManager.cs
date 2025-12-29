using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using System;

public class EdgeDataManager : MonoBehaviour, IInitializable
{
    // [수정] SO 파일 구조에 맞춰 EdgeDataType(재질)을 Key로 사용
    private Dictionary<EdgeDataType, EdgeDataSO> _edgeDataMap;

    private List<AsyncOperationHandle> _handles = new List<AsyncOperationHandle>();
    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<EdgeDataManager>(ManagerScope.Global);

        // [리팩토링 3] 핸들 해제
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
            // 라벨 "EdgeData"로 로드
            var loadHandle = Addressables.LoadAssetsAsync<EdgeDataSO>("EdgeData", null);
            _handles.Add(loadHandle);

            IList<EdgeDataSO> results = await loadHandle.ToUniTask();

            // [리팩토링 6] 필수 데이터 검증
            if (loadHandle.Status != AsyncOperationStatus.Succeeded || results == null || results.Count == 0)
            {
                throw new Exception("[EdgeDataManager] EdgeData 로드 실패.");
            }

            foreach (var edgeData in results)
            {
                // [Fix] SO에 없는 Type 필드는 쓰지 않고, DataType만으로 매핑
                if (edgeData.DataType == EdgeDataType.None) continue;

                if (_edgeDataMap.ContainsKey(edgeData.DataType))
                {
                    Debug.LogWarning($"[EdgeDataManager] 중복 데이터 무시됨: {edgeData.DataType}");
                    continue;
                }
                _edgeDataMap.Add(edgeData.DataType, edgeData);
            }

            _isInitialized = true;
            Debug.Log($"[EdgeDataManager] {_edgeDataMap.Count} Edge Data Loaded.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EdgeDataManager] Initialize Failed: {ex.Message}");
            throw;
        }
    }

    // [중요] 런타임에 구조(Type)와 데이터(SO)를 조합하여 반환
    public EdgeInfo GetEdgeInfo(EdgeType type, EdgeDataType dataType)
    {
        if (!_isInitialized) return EdgeInfo.Open;

        // 재질 데이터가 있으면 가져오고, 없으면 null
        EdgeDataSO data = null;
        if (_edgeDataMap.TryGetValue(dataType, out var foundData))
        {
            data = foundData;
        }
        else if (dataType != EdgeDataType.None)
        {
            Debug.LogWarning($"[EdgeDataManager] EdgeData missing for: {dataType}");
        }

        // EdgeInfo.Create 팩토리 메서드를 통해 구조체 생성 (기존 코드 호환)
        return EdgeInfo.Create(type, dataType, data);
    }
}