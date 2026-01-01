using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class MapManager : MonoBehaviour, IInitializable
{
    private MapCatalogManager _catalogManager;
    private TilemapGenerator _tilemapGenerator;

    // 초기화 여부 체크 변수
    private bool _isInitialized = false;

    private Tile[,,] _tiles;
    public int GridWidth { get; private set; }
    public int GridDepth { get; private set; }
    public int LayerCount { get; private set; }
    public int MinLevel { get; private set; }

    public GridCoords BasePosition { get; private set; }

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
        // 여기서 GetComponent를 하지 않습니다. (다른 오브젝트에 있을 수 있음)
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MapManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        if(_isInitialized==true) return;
        // 1. MapCatalogManager 가져오기
        if (!ServiceLocator.TryGet(out _catalogManager))
        {
            Debug.LogError("[MapManager] MapCatalogManager를 찾을 수 없습니다!");
        }

        // [Fix] TilemapGenerator는 SceneInitializer에 의해 이미 등록되어 있으므로
        // ServiceLocator를 통해 가져옵니다. (AddComponent 방지)
        if (!ServiceLocator.TryGet(out _tilemapGenerator))
        {
            Debug.LogError("[MapManager] TilemapGenerator가 ServiceLocator에 등록되지 않았습니다! SceneInitializer를 확인하세요.");
            // 비상시가 아니면 AddComponent 하지 않음 (중복 등록 방지)
        }

        // 기존 로직 수행
        if (context.MapData != null)
        {
            await LoadMap(context.MapData);
        }

        _isInitialized = true;
    }

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

        // BasePosition 설정
        BasePosition = new GridCoords(mapData.BasePosition.x, mapData.BasePosition.y, MinLevel);

        _tiles = new Tile[GridWidth, GridDepth, LayerCount];

        // 2. 타일 데이터 로드
        foreach (var tileData in mapData.Tiles)
        {
            int localX = tileData.Coords.x - BasePosition.x;
            int localZ = tileData.Coords.z - BasePosition.z;
            int localY = tileData.Coords.y - BasePosition.y;

            if (localX >= 0 && localX < GridWidth &&
                localZ >= 0 && localZ < GridDepth &&
                localY >= 0 && localY < LayerCount)
            {
                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
                tile.LoadFromSaveData(tileData);
                _tiles[localX, localZ, localY] = tile;
            }
        }

        // [Fix] 맵 로드 후, 맵 생성기(Visual) 실행 요청
        // SetupState가 직접 호출하지 않고 여기서 호출하거나, 
        // 혹은 SetupState가 TilemapGenerator를 호출하도록 구조를 유지해도 됩니다.
        // 여기서는 데이터 로드만 담당하고, 비주얼 생성은 SetupState나 TilemapGenerator가 담당하는 것이 깔끔합니다.

        Debug.Log($"[MapManager] Map Loaded Successfully: {mapData.name}");
        return UniTask.CompletedTask;
    }

    public Tile GetTile(GridCoords coords)
    {
        if (_tiles == null) return null;

        int localX = coords.x - BasePosition.x;
        int localZ = coords.z - BasePosition.z;
        int localY = coords.y - BasePosition.y;

        if (localX < 0 || localX >= GridWidth) return null;
        if (localZ < 0 || localZ >= GridDepth) return null;
        if (localY < 0 || localY >= LayerCount) return null;

        return _tiles[localX, localZ, localY];
    }

    public bool HasTile(GridCoords coords) => GetTile(coords) != null;

    public IEnumerable<Tile> GetAllTiles()
    {
        if (_tiles == null) yield break;

        int width = _tiles.GetLength(0);
        int depth = _tiles.GetLength(1);
        int height = _tiles.GetLength(2);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    Tile tile = _tiles[x, z, y];
                    if (tile != null) yield return tile;
                }
            }
        }
    }
}