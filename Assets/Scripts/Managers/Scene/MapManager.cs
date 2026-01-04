using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

public class MapManager : MonoBehaviour, IInitializable
{
    private MapCatalogManager _catalogManager;
    private TilemapGenerator _tilemapGenerator;
    private bool _isInitialized = false;

    private Tile[,,] _tiles;
    private Dictionary<GridCoords, Unit> _unitMap = new Dictionary<GridCoords, Unit>();

    public int GridWidth { get; private set; }
    public int GridDepth { get; private set; }
    public int LayerCount { get; private set; }
    public int MinLevel { get; private set; }
    public GridCoords BasePosition { get; private set; }

    public Vector3 GridToWorld(GridCoords coords) => GridUtils.GridToWorld(coords);
    public GridCoords WorldToGrid(Vector3 pos) => GridUtils.WorldToGrid(pos);

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<MapManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        if (_isInitialized) return;
        ServiceLocator.TryGet(out _catalogManager);
        ServiceLocator.TryGet(out _tilemapGenerator);

        if (context.MapData != null) await LoadMap(context.MapData);
        _isInitialized = true;
    }

    public UniTask LoadMap(MapDataSO mapData)
    {
        _unitMap.Clear();
        if (mapData == null) return UniTask.CompletedTask;

        GridWidth = mapData.GridSize.x;
        GridDepth = mapData.GridSize.y;
        MinLevel = mapData.MinLevel;
        LayerCount = (mapData.MaxLevel - mapData.MinLevel) + 1;
        BasePosition = new GridCoords(mapData.BasePosition.x, mapData.BasePosition.y, MinLevel);
        _tiles = new Tile[GridWidth, GridDepth, LayerCount];

        foreach (var tileData in mapData.Tiles)
        {
            int localX = tileData.Coords.x - BasePosition.x;
            int localZ = tileData.Coords.z - BasePosition.z;
            int localY = tileData.Coords.y - BasePosition.y;

            if (localX >= 0 && localX < GridWidth && localZ >= 0 && localZ < GridDepth && localY >= 0 && localY < LayerCount)
            {
                // [Fix] RoleTag 주입
                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID, tileData.RoleTag);
                tile.LoadFromSaveData(tileData);
                _tiles[localX, localZ, localY] = tile;
            }
        }
        Debug.Log($"[MapManager] Map Loaded: {mapData.name}");
        return UniTask.CompletedTask;
    }

    public Tile GetTile(GridCoords coords)
    {
        int localX = coords.x - BasePosition.x;
        int localZ = coords.z - BasePosition.z;
        int localY = coords.y - BasePosition.y;
        if (localX < 0 || localX >= GridWidth || localZ < 0 || localZ >= GridDepth || localY < 0 || localY >= LayerCount) return null;
        return _tiles[localX, localZ, localY];
    }

    public bool HasTile(GridCoords coords) => GetTile(coords) != null;
    public IEnumerable<Tile> GetAllTiles()
    {
        if (_tiles == null) yield break;
        foreach (var tile in _tiles) if (tile != null) yield return tile;
    }

    // ========================================================================
    // 유닛 관리
    // ========================================================================
    public bool HasUnit(GridCoords coords) => _unitMap.ContainsKey(coords);

    public void RegisterUnit(GridCoords coords, Unit unit)
    {
        if (_unitMap.ContainsKey(coords))
            Debug.LogWarning($"[MapManager] Overwriting unit at {coords}");

        _unitMap[coords] = unit;
        GetTile(coords)?.AddOccupant(unit); // Tile 점유 동기화
    }

    public void UnregisterUnit(Unit unit)
    {
        if (unit == null) return;
        GridCoords coords = unit.Coordinate;

        if (_unitMap.ContainsKey(coords) && _unitMap[coords] == unit)
        {
            _unitMap.Remove(coords);
        }
        GetTile(coords)?.RemoveOccupant(unit); // Tile 점유 해제
    }

    public void MoveUnit(Unit unit, GridCoords newCoords)
    {
        UnregisterUnit(unit);
        RegisterUnit(newCoords, unit);
    }

    public bool TryGetRandomTileByTag(string tag, out Tile tile)
    {
        if (string.IsNullOrEmpty(tag)) { tile = null; return false; }

        // [Fix] Tile.RoleTag 사용 가능
        var candidates = GetAllTiles()
            .Where(t => t.RoleTag == tag && t.IsWalkable && !HasUnit(t.Coordinate))
            .ToList();

        if (candidates.Count == 0) { tile = null; return false; }
        tile = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }
}