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

    // Getter 프로퍼티
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
            Debug.Log($"[MapManager] Starting load for map: {mapData.DisplayName}");

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // [Step 1] 기본 설정값 로드 및 초기 로그 [개선점 1 반영]
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            int declaredWidth = mapData.GridSize.x;
            int declaredDepth = mapData.GridSize.y;

            _minLevel = mapData.MinLevel;
            _layerCount = mapData.MaxLevel - mapData.MinLevel + 1;

            Debug.Log($"[MapManager] Config - Grid: {declaredWidth}x{declaredDepth}, Layers: {_layerCount}, Tiles in data: {mapData.Tiles.Count}");

            // [Step 2] 빈 맵 처리 [개선점 2 반영]
            if (mapData.Tiles.Count == 0)
            {
                Debug.LogWarning("[MapManager] MapData has no tiles. Creating empty map with declared size.");
                _gridWidth = declaredWidth;
                _gridDepth = declaredDepth;
                _tiles = new Tile[_gridWidth, _gridDepth, _layerCount];
                return;
            }

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // [Step 3] 경계 스캔 및 자동 보정 (Bounds Check)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            int maxX = declaredWidth - 1;
            int maxZ = declaredDepth - 1;
            bool hasMismatch = false;

            foreach (var tileData in mapData.Tiles)
            {
                if (tileData.Coords.x > maxX || tileData.Coords.z > maxZ)
                {
                    hasMismatch = true;
                    maxX = Mathf.Max(maxX, tileData.Coords.x);
                    maxZ = Mathf.Max(maxZ, tileData.Coords.z);
                }
            }

            if (hasMismatch)
            {
                _gridWidth = maxX + 1;
                _gridDepth = maxZ + 1;

                Debug.LogWarning(
                    $"[MapManager] GridSize mismatch detected. " +
                    $"Declared: {declaredWidth}x{declaredDepth}, Actual range: (0,0) to ({maxX},{maxZ}). " +
                    $"Corrected to: {_gridWidth}x{_gridDepth}. " +
                    $"Please update MapDataSO.GridSize to match this size.");
            }
            else
            {
                _gridWidth = declaredWidth;
                _gridDepth = declaredDepth;
            }

            _tiles = new Tile[_gridWidth, _gridDepth, _layerCount];

            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            // [Step 4] 타일 객체 생성 (Time-Sliced)
            // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int loadedCount = 0;
            int skippedCount = 0; // [개선점 3 반영]

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

                if (IsOutOfBounds(x, z, levelIndex))
                {
                    skippedCount++;
                    Debug.LogError($"[MapManager] Tile {tileData.Coords} is still out of bounds. Skipping.");
                    continue;
                }

                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
                tile.LoadFromSaveData(tileData);

                _tiles[x, z, levelIndex] = tile;
                loadedCount++;
            }

            // 최종 리포트 [개선점 3 반영]
            string finalStatus = $"[MapManager] Map load completed. Total: {loadedCount}";
            if (skippedCount > 0)
            {
                finalStatus += $", Skipped: {skippedCount}";
                Debug.LogWarning(finalStatus);
            }
            else
            {
                Debug.Log(finalStatus);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MapManager] LoadMap failed: {ex.Message}");
            throw;
        }
    }

    public Tile GetTile(GridCoords coords)
    {
        if (!_isInitialized) return null;

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