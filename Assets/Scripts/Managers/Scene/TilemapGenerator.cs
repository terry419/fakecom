using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic; // List 사용을 위해 추가

public class TilemapGenerator : MonoBehaviour, IInitializable
{
    private Transform _mapContainer;
    private List<GameObject> _spawnedObjects = new List<GameObject>();

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<TilemapGenerator>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            // [구현] 맵 오브젝트들을 담을 부모 트랜스폼 생성
            GameObject containerGo = new GameObject("@MapContainer");
            _mapContainer = containerGo.transform;
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TilemapGenerator] Initialize Error: {ex.Message}");
            throw;
        }
    }

    // [변경] Generate() -> GenerateAsync() (개선점 3: 비동기 및 프레임 드랍 방지)
    public async UniTask GenerateAsync()
    {
        Debug.Log("[TilemapGenerator] Start generating visuals (Async)...");

        MapManager mapManager = null;
        TileDataManager tileDataManager = null;

        // [개선점 2 반영] ServiceLocator 예외 명시적 처리
        try
        {
            mapManager = ServiceLocator.Get<MapManager>();
            tileDataManager = ServiceLocator.Get<TileDataManager>();
        }
        catch (InvalidOperationException ex)
        {
            Debug.LogError($"[TilemapGenerator] Failed to get dependencies: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TilemapGenerator] Unexpected error during dependency resolution: {ex.Message}");
            return;
        }

        if (mapManager == null || tileDataManager == null)
        {
            Debug.LogError("[TilemapGenerator] Managers are missing (Available but returned null)!");
            return;
        }

        ClearMap();

        int spawnedCount = 0;

        // MapManager의 그리드 정보 순회
        for (int x = 0; x < mapManager.GridWidth; x++)
        {
            for (int z = 0; z < mapManager.GridDepth; z++)
            {
                for (int y = 0; y < mapManager.LayerCount; y++)
                {
                    GridCoords coords = new GridCoords(x, z, mapManager.MinLevel + y);
                    Tile tile = mapManager.GetTile(coords);

                    // 빈 타일 건너뜀
                    if (tile == null) continue;

                    // 바닥 생성
                    if (tile.FloorID != FloorType.None)
                    {
                        CreateVisual(tileDataManager, tile.FloorID, coords);
                        spawnedCount++;
                    }

                    // [개선점 3] 100개 생성 시마다 프레임 양보 (메인 스레드 프리징 방지)
                    if (spawnedCount % 100 == 0)
                    {
                        await UniTask.Yield();
                    }
                }
            }
        }

        Debug.Log($"[TilemapGenerator] Async Generation Complete. Spawned: {spawnedCount}");
    }

    private void CreateVisual(TileDataManager dataMgr, FloorType type, GridCoords coords)
    {
        // [수정] 새로 만든 안전한 메서드 사용
        GameObject prefab = dataMgr.GetFloorPrefab(type);

        if (prefab != null)
        {
            Vector3 worldPos = GridUtils.GridToWorld(coords);
            GameObject instance = Instantiate(prefab, _mapContainer);
            instance.transform.position = worldPos;
            instance.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";
            _spawnedObjects.Add(instance);
        }
    }

    public void ClearMap()
    {
        foreach (var obj in _spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        _spawnedObjects.Clear();
    }
}