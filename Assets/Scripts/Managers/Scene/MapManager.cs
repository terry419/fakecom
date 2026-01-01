using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class MapManager : MonoBehaviour, IInitializable
{
    private MapCatalogManager _catalogManager;
    private TilemapGenerator _tilemapGenerator;

    private Tile[,,] _tiles;
    public int GridWidth { get; private set; }
    public int GridDepth { get; private set; }
    public int LayerCount { get; private set; }
    public int MinLevel { get; private set; }

    public GridCoords BasePosition { get; private set; }

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MapManager>(ManagerScope.Scene);
    }

    // [CS1998 해결] async 제거, 완료된 Task 반환
    public UniTask Initialize(InitializationContext context)
    {
        _catalogManager = ServiceLocator.Get<MapCatalogManager>();
        _tilemapGenerator = ServiceLocator.Get<TilemapGenerator>();
        return UniTask.CompletedTask;
    }

    // [CS1998 해결] async 제거
    public UniTask LoadMap(MapDataSO mapData)
    {
        if (mapData == null)
        {
            Debug.LogError("[MapManager] MapData is null.");
            return UniTask.CompletedTask;
        }

        // 1. Grid 설정
        GridWidth = mapData.GridSize.x;
        GridDepth = mapData.GridSize.y;
        MinLevel = mapData.MinLevel;
        LayerCount = (mapData.MaxLevel - mapData.MinLevel) + 1;

        // [GridCoords 순서 확정] (x, z, y)
        // MapDataSO.BasePosition은 Vector2Int(x, y)이므로, y값을 z 인자에 넣습니다.
        // 3번째 인자(Height)에는 MinLevel을 넣습니다.
        BasePosition = new GridCoords(mapData.BasePosition.x, mapData.BasePosition.y, MinLevel);

        _tiles = new Tile[GridWidth, GridDepth, LayerCount];

        // 2. 논리적 타일 데이터 생성
        foreach (var tileData in mapData.Tiles)
        {
            // [좌표 변환] BasePosition 오프셋 차감
            // GridCoords의 필드: x, z(Depth), y(Height)
            int localX = tileData.Coords.x - BasePosition.x;
            int localZ = tileData.Coords.z - BasePosition.z;
            int localY = tileData.Coords.y - BasePosition.y;

            // 배열 범위 체크
            if (localX >= 0 && localX < GridWidth &&
                localZ >= 0 && localZ < GridDepth &&
                localY >= 0 && localY < LayerCount)
            {
                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
                tile.LoadFromSaveData(tileData);
                _tiles[localX, localZ, localY] = tile;
            }
        }

        Debug.Log($"[MapManager] Map Loaded Successfully: {mapData.name}");
        return UniTask.CompletedTask;
    }

    public Tile GetTile(GridCoords coords)
    {
        if (_tiles == null) return null;

        // Global Coords -> Local Indices
        int localX = coords.x - BasePosition.x;
        int localZ = coords.z - BasePosition.z;
        int localY = coords.y - BasePosition.y;

        if (localX < 0 || localX >= GridWidth) return null;
        if (localZ < 0 || localZ >= GridDepth) return null;
        if (localY < 0 || localY >= LayerCount) return null;

        return _tiles[localX, localZ, localY];
    }

    // 좌표 유효성 검사 헬퍼
    public bool HasTile(GridCoords coords)
    {
        return GetTile(coords) != null;
    }

    // [최적화] IEnumerable을 사용하여 GC Alloc 최소화
    public IEnumerable<Tile> GetAllTiles()
    {
        if (_tiles == null) yield break;

        int width = _tiles.GetLength(0);
        int depth = _tiles.GetLength(1);
        int height = _tiles.GetLength(2);

        // 3차원 배열 순회
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = _tiles[x, z, y];
                    if (tile != null)
                    {
                        yield return tile;
                    }
                }
            }
        }
    }
}