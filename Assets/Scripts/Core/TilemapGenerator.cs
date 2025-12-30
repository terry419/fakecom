using Cysharp.Threading.Tasks;
using UnityEngine;

public class TilemapGenerator
{
    private readonly MapManager _mapManager;
    private readonly TileDataManager _tileDataManager;
    private readonly Transform _mapRoot;

    public TilemapGenerator()
    {
        _mapManager = ServiceLocator.Get<MapManager>();
        _tileDataManager = ServiceLocator.Get<TileDataManager>();

        GameObject rootGo = GameObject.Find("MapRoot");
        if (rootGo == null)
            rootGo = new GameObject("MapRoot");
        _mapRoot = rootGo.transform;
    }

    public async UniTask GenerateAsync()
    {
        if (_mapManager.GridWidth == 0)
        {
            Debug.LogWarning("[TilemapGenerator] Map dimensions are zero.");
            return;
        }

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        const int frameTimeBudgetMs = 8;

        for (int y = 0; y < _mapManager.LayerCount; y++)
        {
            for (int z = 0; z < _mapManager.GridDepth; z++)
            {
                for (int x = 0; x < _mapManager.GridWidth; x++)
                {
                    if (stopwatch.ElapsedMilliseconds > frameTimeBudgetMs)
                    {
                        await UniTask.Yield();
                        stopwatch.Restart();
                    }
                    
                    int currentLevel = y + _mapManager.MinLevel;
                    var coords = new GridCoords(x, y: currentLevel, z: z);
                    
                    Tile tile = _mapManager.GetTile(coords);
                    if (tile == null) continue;

                    // [수정] tile.FloorId -> tile.FloorID
                    if (tile.FloorID != FloorType.None)
                    {
                        if (_tileDataManager.TryGetFloorVisual(tile.FloorID, out GameObject prefab))
                        {
                            if (prefab != null)
                            {
                                Vector3 worldPos = GridUtils.GridToWorld(coords);
                                Object.Instantiate(prefab, worldPos, Quaternion.identity, _mapRoot);
                            }
                        }
                    }

                    // [수정] tile.PillarId -> tile.PillarID
                    if (tile.PillarID != PillarType.None)
                    {
                        if (_tileDataManager.TryGetPillarVisual(tile.PillarID, out GameObject pillarPrefab))
                        {
                            if (pillarPrefab != null)
                            {
                                Vector3 worldPos = GridUtils.GridToWorld(coords);
                                Object.Instantiate(pillarPrefab, worldPos, Quaternion.identity, _mapRoot);
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log($"[TilemapGenerator] Map generation complete.");
    }
}