using UnityEngine;
using Cysharp.Threading.Tasks;

public class MapTestLoader : MonoBehaviour
{
    [Header("테스트할 맵 데이터 드래그 앤 드롭")]
    public MapDataSO targetMapData;

    private async void Start()
    {
        // 1. 시스템 초기화 대기 (다른 매니저들이 다 뜰 때까지 잠깐 대기)
        await UniTask.Yield(PlayerLoopTiming.Update);

        var mapManager = ServiceLocator.Get<MapManager>();

        if (mapManager == null)
        {
            Debug.LogError(" MapTestLoader: MapManager를 찾을 수 없습니다.");
            return;
        }

        if (targetMapData == null)
        {
            Debug.LogError(" MapTestLoader: 테스트할 MapDataSO가 할당되지 않았습니다. 인스펙터를 확인하세요.");
            return;
        }

        Debug.Log($" [TEST] 강제 맵 로드 시작: {targetMapData.name}");

        // 2. 맵 데이터 로드
        await mapManager.LoadMap(targetMapData);

        // 3. 비주얼 생성 (TilemapGenerator가 자동으로 호출되지 않는 구조라면 여기서 수동 호출)
        var tilemapGenerator = ServiceLocator.Get<TilemapGenerator>();
        if (tilemapGenerator != null)
        {
            Debug.Log(" [TEST] 비주얼 생성 요청...");
            await tilemapGenerator.GenerateAsync(); // 혹은 GenerateMap(mapManager) 등 함수명에 맞춰 호출
        }
        else
        {
            Debug.LogWarning(" TilemapGenerator가 없습니다. 데이터만 로드됩니다.");
        }
    }
}