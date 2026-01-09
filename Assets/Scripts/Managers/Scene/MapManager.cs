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
    public int StateVersion { get; private set; } = 0;
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

        Debug.Log($"[MapManager] Start Loading Map: {mapData.name}..."); // [Debug] 시작 로그

        GridWidth = mapData.GridSize.x;
        GridDepth = mapData.GridSize.y;
        MinLevel = mapData.MinLevel;
        LayerCount = (mapData.MaxLevel - mapData.MinLevel) + 1;
        BasePosition = new GridCoords(mapData.BasePosition.x, mapData.BasePosition.y, MinLevel);
        _tiles = new Tile[GridWidth, GridDepth, LayerCount];

        int loadedTagsCount = 0;

        foreach (var tileData in mapData.Tiles)
        {
            int localX = tileData.Coords.x - BasePosition.x;
            int localZ = tileData.Coords.z - BasePosition.z;
            int localY = tileData.Coords.y - BasePosition.y;

            if (localX >= 0 && localX < GridWidth && localZ >= 0 && localZ < GridDepth && localY >= 0 && localY < LayerCount)
            {
                // 1. 타일 생성 (생성자 주입)
                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID, tileData.RoleTag);

                // 2. 데이터 로드
                tile.LoadFromSaveData(tileData);

                // 3. [Debug Fix] 강제 주입 및 로깅
                if (!string.IsNullOrEmpty(tileData.RoleTag))
                {
                    // 데이터에는 있는데 타일에는 없으면 강제 주입
                    if (string.IsNullOrEmpty(tile.RoleTag))
                    {
                        tile.ForceSetRoleTag(tileData.RoleTag);
                        Debug.LogError($"[CRITICAL FIX] Tile {tile.Coordinate} missing tag! Forced: '{tileData.RoleTag}'");
                    }

                    // 정상적으로 들어갔는지 최종 확인
                    if (tile.RoleTag == tileData.RoleTag)
                    {
                        loadedTagsCount++;
                        // 너무 많이 찍히면 시끄러우니 첫 5개만 상세 출력
                        if (loadedTagsCount <= 5)
                            Debug.Log($"[MapManager] Tag OK: {tile.Coordinate} = '{tile.RoleTag}'");
                    }
                }

                _tiles[localX, localZ, localY] = tile;
            }
        }

        Debug.Log($"[MapManager] Load Complete. Total Tiles with Tags: {loadedTagsCount}");

        // [Debug] 맵 전체 데이터 덤프 (진단용)
        DebugDumpMapData();

        return UniTask.CompletedTask;
    }

    // [New] 맵 데이터 진단 함수
    private void DebugDumpMapData()
    {
        Debug.Log("--- [MapManager] MAP DATA DUMP START ---");
        int tagCount = 0;
        foreach (var tile in GetAllTiles())
        {
            if (!string.IsNullOrEmpty(tile.RoleTag))
            {
                Debug.Log($"[DUMP] Tile {tile.Coordinate} : Tag='{tile.RoleTag}' | Walkable={tile.IsWalkable}");
                tagCount++;
            }
        }
        Debug.Log($"--- [MapManager] MAP DATA DUMP END (Found {tagCount} tags) ---");
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

    // 유닛 관리 메서드들... (기존 유지)
    public bool HasUnit(GridCoords coords) => _unitMap.ContainsKey(coords);
    public void RegisterUnit(GridCoords coords, Unit unit)
    {
        if (_unitMap.ContainsKey(coords)) Debug.LogWarning($"[MapManager] Overwriting unit at {coords}");
        _unitMap[coords] = unit;
        GetTile(coords)?.AddOccupant(unit);
    }
    public void UnregisterUnit(Unit unit)
    {
        if (unit == null) return;
        GridCoords coords = unit.Coordinate;
        if (_unitMap.ContainsKey(coords) && _unitMap[coords] == unit) _unitMap.Remove(coords);
        GetTile(coords)?.RemoveOccupant(unit);
    }
    public Unit GetUnit(GridCoords coords) => _unitMap.TryGetValue(coords, out Unit unit) ? unit : null;
    public void MoveUnit(Unit unit, GridCoords newCoords) { UnregisterUnit(unit); RegisterUnit(newCoords, unit); }

    public bool TryGetRandomTileByTag(string tag, out Tile tile)
    {
        if (string.IsNullOrEmpty(tag)) { tile = null; return false; }

        // [Debug] 검색 요청 로그
        Debug.Log($"[MapManager] Searching for tag: '{tag}'...");

        var candidates = GetAllTiles()
            .Where(t => t.RoleTag == tag && t.IsWalkable && !HasUnit(t.Coordinate))
            .ToList();

        Debug.Log($"[MapManager] Found {candidates.Count} candidates for '{tag}'");

        if (candidates.Count == 0) { tile = null; return false; }
        tile = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        return true;
    }

    public void NotifyMapChanged() => StateVersion++;
}