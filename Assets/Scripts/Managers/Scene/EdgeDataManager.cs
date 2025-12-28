using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets; // 필수
using UnityEngine.ResourceManagement.AsyncOperations; // 필수

public class EdgeDataManager : MonoBehaviour, IInitializable
{
    // 데이터 저장소
    private Dictionary<EdgeDataType, EdgeDataSO> _library = new();

    // [삭제] Inspector 할당 변수 제거 (더 이상 씬에 배치하지 않으므로 불필요)
    // [SerializeField] private EdgeDataSO[] _defaultEdgeDataSOs; 

    public async UniTask Initialize(InitializationContext context)
    {
        _library.Clear();

        // 1. "EdgeData"라는 라벨이 붙은 모든 에셋을 로드합니다.
        // (주의: SO 파일들에 'EdgeData' 라벨을 붙여야 함)
        var handle = Addressables.LoadAssetsAsync<EdgeDataSO>("EdgeData", (loadedSO) =>
        {
            // 콜백: 에셋이 하나씩 로드될 때마다 실행됨
            if (loadedSO != null && loadedSO.DataType != EdgeDataType.None)
            {
                if (!_library.ContainsKey(loadedSO.DataType))
                {
                    _library.Add(loadedSO.DataType, loadedSO);
                }
            }
        });

        // 2. 로딩이 끝날 때까지 대기
        await handle.ToUniTask();

        // 3. 데이터 검증 (필수 데이터가 다 들어왔나?)
        ValidateAllDataLoaded();

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

    public EdgeDataSO GetData(EdgeDataType type)
    {
        if (type == EdgeDataType.None) return null;
        if (_library.TryGetValue(type, out var data)) return data;
        return null;
    }
}