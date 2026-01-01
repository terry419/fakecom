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

        // [수정 핵심] MapManager.BasePosition이 GridCoords(x, z, y)로 변경되었습니다.
        // 기존: mapManager.BasePosition.y (Vector2Int 시절, y가 깊이였음)
        // 변경: mapManager.BasePosition.z (GridCoords, z가 깊이임)
        int baseX = mapManager.BasePosition.x;
        int baseZ = mapManager.BasePosition.z;

        // MapManager 그리드 크기만큼 순회
        for (int x = 0; x < mapManager.GridWidth; x++)
        {
            for (int z = 0; z < mapManager.GridDepth; z++)
            {
                for (int y = 0; y < mapManager.LayerCount; y++)
                {
                    // Local Index(x,z) + BasePosition => World Grid Coordinates
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
                            // 기둥은 Direction 의미 없음 (North 기본값)
                            CreateStructureVisual(pillarEntry.Prefab, coords, Direction.North, hp, true);
                            spawnedCount++;
                        }
                    }

                    // 3. 벽/창문 (Edge) 생성
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

                    // 비동기 부하 분산 (100개마다 프레임 양보)
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

            if (instance.GetComponent<Collider>() == null)
            {
                instance.AddComponent<BoxCollider>();
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
            // 벽 회전 처리 (East/West는 90도 회전)
            instance.transform.rotation = (dir == Direction.North || dir == Direction.South)
                ? Quaternion.identity
                : Quaternion.Euler(0, 90, 0);
        }

        // 높이 보정
        AlignToGround(instance, pos.y);

        instance.name = isPillar ? $"Pillar_{coords}" : $"Wall_{coords}_{dir}";

        var structure = instance.GetComponent<StructureObj>();
        if (structure == null) structure = instance.AddComponent<StructureObj>();

        structure.Initialize(coords, dir, hp, isPillar);

        if (instance.GetComponent<Collider>() == null)
        {
            instance.AddComponent<BoxCollider>();
        }

        int layer = isPillar ? LayerMask.NameToLayer("Pillar") : LayerMask.NameToLayer("Wall");
        if (layer > -1) instance.layer = layer;

        _spawnedObjects.Add(instance);
    }

    private void AlignToGround(GameObject obj, float targetY)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        float currentMinY = combinedBounds.min.y;
        float diff = targetY - currentMinY;
        obj.transform.position += new Vector3(0, diff, 0);
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