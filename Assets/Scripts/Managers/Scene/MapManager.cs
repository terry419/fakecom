using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;

public class MapManager : MonoBehaviour, IInitializable
{
    // GDD 5.6: 3차원 배열 (Tile[x, z, y])
    private Tile[,,] _tiles;
    private Vector3Int _mapDimensions;
    private int _minLevel;

    public async UniTask Initialize(InitializationContext context)
    {
        // 초기화 시점에는 아직 맵 데이터가 없을 수 있음
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// [비평가 반영] MapDataSO를 읽어서 실제 메모리에 타일 맵 구축
    /// </summary>
    public async UniTask LoadMap(MapDataSO mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("[MapManager] MapDataSO is null!");
            return;
        }

        Debug.Log($"[MapManager] Loading Map: {mapData.DisplayName} ({mapData.GridSize.x}x{mapData.GridSize.y})");

        // 1. 맵 차원 설정
        int width = mapData.GridSize.x;
        int depth = mapData.GridSize.y;
        int height = mapData.MaxLevel - mapData.MinLevel + 1;
        _minLevel = mapData.MinLevel;
        _mapDimensions = new Vector3Int(width, depth, height);

        // 2. 배열 할당
        _tiles = new Tile[width, depth, height];

        // 3. 데이터 로드 (Time-Sliced Streaming)
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        int loadedCount = 0;

        foreach (var tileData in mapData.Tiles)
        {
            // 16ms 제한 (프레임 드랍 방지)
            if (stopwatch.ElapsedMilliseconds > 16)
            {
                await UniTask.Yield();
                stopwatch.Restart();
            }

            int x = tileData.Coords.x;
            int z = tileData.Coords.z;
            int arrayY = tileData.Coords.y - _minLevel;

            // 범위 체크
            if (IsOutOfBounds(x, z, arrayY)) continue;

            // 타일 생성 및 데이터 주입
            Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
            tile.LoadFromSaveData(tileData);

            _tiles[x, z, arrayY] = tile;
            loadedCount++;
        }

        Debug.Log($"[MapManager] Map Loaded. {loadedCount} tiles created.");

        // 4. 스폰 포인트 등 추가 로직 처리...
    }

    public Tile GetTile(GridCoords coords)
    {
        int arrayY = coords.y - _minLevel;
        if (IsOutOfBounds(coords.x, coords.z, arrayY)) return null;
        return _tiles[coords.x, coords.z, arrayY];
    }

    private bool IsOutOfBounds(int x, int z, int arrayY)
    {
        return x < 0 || x >= _mapDimensions.x ||
               z < 0 || z >= _mapDimensions.y ||
               arrayY < 0 || arrayY >= _mapDimensions.z;
    }
}