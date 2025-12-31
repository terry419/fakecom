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

        // MapManager의 그리드 정보 순회
        for (int x = 0; x < mapManager.GridWidth; x++)
        {
            for (int z = 0; z < mapManager.GridDepth; z++)
            {
                for (int y = 0; y < mapManager.LayerCount; y++)
                {
                    GridCoords coords = new GridCoords(x, z, mapManager.MinLevel + y);
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

                            // [Fix] EdgeType.None 제거 -> Open이 아니면 벽이 있는 것으로 간주
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

        // [Fix] StructureObj 참조 에러 해결을 위해 아래 StructureObj.cs 파일을 생성해야 합니다.
        var structure = instance.GetComponent<StructureObj>();
        if (structure == null) structure = instance.AddComponent<StructureObj>();

        structure.Initialize(coords, dir, hp, isPillar);

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