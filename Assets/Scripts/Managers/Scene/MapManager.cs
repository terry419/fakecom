using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
{
    private const int FRAME_TIME_BUDGET_MS = 16;

    private Tile[,,] _tiles;

    private int _gridWidth;
    private int _gridDepth;
    private int _layerCount;
    private int _minLevel;

    private bool _isInitialized = false;

    public int GridWidth => _gridWidth;
    public int GridDepth => _gridDepth;
    public int LayerCount => _layerCount;
    public int MinLevel => _minLevel;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MapManager>(ManagerScope.Scene);
        _isInitialized = false;
    }

    public async UniTask Initialize(InitializationContext context)
    {
        Debug.Log($"[MapManager] Initialize called. Context has map data: {context.HasMapData}");
        if (context.MapData != null)
        {
            await LoadMap(context.MapData);
        }
        _isInitialized = true;
    }

    public async UniTask LoadMap(MapDataSO mapData)
    {
        if (mapData == null)
            throw new ArgumentNullException(nameof(mapData), "[MapManager] MapDataSO is null.");

        try
        {
            Debug.Log($"[MapManager] Loading Map: {mapData.DisplayName}");

            _gridWidth = mapData.GridSize.x;
            _gridDepth = mapData.GridSize.y;
            _layerCount = mapData.MaxLevel - mapData.MinLevel + 1;
            _minLevel = mapData.MinLevel;

            Debug.Log($"[MapManager] Map Dimensions: Width={_gridWidth}, Depth={_gridDepth}, Layers={_layerCount}, MinLevel={_minLevel}"); // ADDED LOG

            _tiles = new Tile[_gridWidth, _gridDepth, _layerCount];

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int loadedCount = 0;

            Debug.Log($"[MapManager] Starting tile creation loop for {mapData.Tiles.Count} tiles...");
            foreach (var tileData in mapData.Tiles)
            {
                if (stopwatch.ElapsedMilliseconds > FRAME_TIME_BUDGET_MS)
                {
                    await UniTask.Yield();
                    stopwatch.Restart();
                }

                int x = tileData.Coords.x;
                int z = tileData.Coords.z;
                int levelIndex = tileData.Coords.y - _minLevel;

                Debug.Log($"[MapManager] Processing tile: {tileData.Coords}. Converted levelIndex: {levelIndex}."); // ADDED LOG
                Debug.Log($"[MapManager] IsOutOfBounds check: x={x} < 0 || x={x} >= {_gridWidth} || z={z} < 0 || z={z} >= {_gridDepth} || levelIndex={levelIndex} < 0 || levelIndex={levelIndex} >= {_layerCount}"); // ADDED LOG

                if (IsOutOfBounds(x, z, levelIndex))
                {
                    Debug.LogWarning($"[MapManager] Tile {tileData.Coords} is out of bounds. Skipping."); // ADDED LOG
                    continue;
                }

                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
                tile.LoadFromSaveData(tileData);

                _tiles[x, z, levelIndex] = tile;
                loadedCount++;
                
                if (loadedCount % 50 == 0)
                {
                    Debug.Log($"[MapManager] ...{loadedCount} tiles created so far.");
                }
            }

            Debug.Log($"[MapManager] Map Loaded. Tiles: {loadedCount}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MapManager] LoadMap Failed: {ex.Message}");
            throw;
        }
    }

    public Tile GetTile(GridCoords coords)
    {
        if (!_isInitialized)
        {
            Debug.LogError("[MapManager] Not initialized yet.");
            return null;
        }

        int levelIndex = coords.y - _minLevel;
        if (IsOutOfBounds(coords.x, coords.z, levelIndex)) return null;

        return _tiles[coords.x, coords.z, levelIndex];
    }

    private bool IsOutOfBounds(int x, int z, int levelIndex)
    {
        return x < 0 || x >= _gridWidth ||
               z < 0 || z >= _gridDepth ||
               levelIndex < 0 || levelIndex >= _layerCount;
    }
}