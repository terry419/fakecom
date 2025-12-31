using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

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

    public async UniTask GenerateAsync()
    {
        Debug.Log("[TilemapGenerator] Start generating visuals (Async)...");

        MapManager mapManager = null;
        TileDataManager tileDataManager = null;

        try
        {
            mapManager = ServiceLocator.Get<MapManager>();
            tileDataManager = ServiceLocator.Get<TileDataManager>();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TilemapGenerator] Dependency Error: {ex.Message}");
            return;
        }

        if (mapManager == null || tileDataManager == null)
        {
            Debug.LogError("[TilemapGenerator] Managers are missing!");
            return;
        }

        ClearMap();

        int spawnedCount = 0;

        // [핵심 수정] BasePosition을 가져옵니다. (예: 18, 6)
        int baseX = mapManager.BasePosition.x;
        int baseZ = mapManager.BasePosition.y; // Vector2Int에서 y가 Z축(Depth) 역할

        // MapManager의 그리드 크기만큼 순회
        for (int x = 0; x < mapManager.GridWidth; x++)
        {
            for (int z = 0; z < mapManager.GridDepth; z++)
            {
                for (int y = 0; y < mapManager.LayerCount; y++)
                {
                    // [핵심 수정] Local Index(x,z)에 BasePosition을 더해 World 좌표로 복구합니다.
                    // 기존 코드: new GridCoords(x, z, ...) -> 틀림 (0,0 호출됨)
                    // 수정 코드: new GridCoords(baseX + x, baseZ + z, ...) -> 맞음 (18,6 호출됨)
                    GridCoords coords = new GridCoords(baseX + x, baseZ + z, mapManager.MinLevel + y);

                    Tile tile = mapManager.GetTile(coords);

                    if (tile == null) continue;

                    // 1. 바닥 (Floor) 생성
                    if (tile.FloorID != FloorType.None && tile.FloorID != FloorType.Void)
                    {
                        CreateVisual(tileDataManager, tile.FloorID, coords);
                        spawnedCount++;
                    }

                    // 2. 기둥 (Pillar) 생성
                    if (tile.InitialPillarID != PillarType.None)
                    {
                        var pillarEntry = tileDataManager.GetPillarData(tile.InitialPillarID);
                        if (pillarEntry.Prefab != null)
                        {
                            float hp = tile.InitialPillarHP > 0 ? tile.InitialPillarHP : pillarEntry.MaxHP;
                            CreateStructureVisual(pillarEntry.Prefab, coords, Direction.North, hp, true);
                            spawnedCount++;
                        }
                    }

                    // 3. 벽 (Edge) 생성
                    if (tile.TempSavedEdges != null)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            var edgeInfo = tile.TempSavedEdges[i];
                            if (edgeInfo.Type != EdgeType.Open)
                            {
                                var edgeEntry = tileDataManager.GetEdgeData(edgeInfo.Type);
                                if (edgeEntry.Prefab != null)
                                {
                                    float hp = edgeInfo.CurrentHP;
                                    CreateStructureVisual(edgeEntry.Prefab, coords, (Direction)i, hp, false);
                                    spawnedCount++;
                                }
                            }
                        }
                    }

                    // 프레임 드랍 방지 (100개마다 대기)
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
        GameObject prefab = dataMgr.GetFloorPrefab(type);
        if (prefab != null)
        {
            Vector3 worldPos = GridUtils.GridToWorld(coords);
            GameObject instance = Instantiate(prefab, _mapContainer);
            instance.transform.position = worldPos;
            instance.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";

            // 바닥에는 Collider가 있어야 Raycast가 됩니다. 프리팹 확인 필요.
            if (instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<BoxCollider>(); // 임시 조치
            }

            _spawnedObjects.Add(instance);
        }
    }

    private void CreateStructureVisual(GameObject prefab, GridCoords coords, Direction dir, float hp, bool isPillar)
    {
        if (prefab == null) return;

        Vector3 pos = GridUtils.GetEdgeWorldPosition(coords, dir);
        if (isPillar) pos = GridUtils.GridToWorld(coords);

        GameObject instance = Instantiate(prefab, _mapContainer);
        instance.transform.position = pos;

        if (!isPillar)
        {
            instance.transform.rotation = (dir == Direction.North || dir == Direction.South)
                ? Quaternion.identity
                : Quaternion.Euler(0, 90, 0);
        }

        instance.name = isPillar ? $"Pillar_{coords}" : $"Wall_{coords}_{dir}";

        var structure = instance.GetComponent<StructureObj>();
        if (structure == null) structure = instance.AddComponent<StructureObj>();

        structure.Initialize(coords, dir, hp, isPillar);

        // 구조물에도 Collider 확인
        if (instance.GetComponent<Collider>() == null)
        {
            // 모델 모양에 따라 다르겠지만 일단 BoxCollider 추가
            instance.AddComponent<BoxCollider>();
        }

        int layer = isPillar ? LayerMask.NameToLayer("Pillar") : LayerMask.NameToLayer("Wall");
        if (layer > -1) instance.layer = layer;

        _spawnedObjects.Add(instance);
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