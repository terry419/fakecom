using UnityEngine;
using System.Collections.Generic;

public class MapEditorContext : ScriptableObject
{
    private static MapEditorContext _instance;
    public static MapEditorContext Instance => _instance ??= CreateInstance<MapEditorContext>();

    public TileRegistrySO Registry;
    public MapDataSO TargetMapData;

    // [Logic Check] 모드를 명확히 분리하여 로직 꼬임 방지
    public enum ToolMode { Tile, Edge, Pillar, Portal, Spawn, Erase }
    public ToolMode CurrentToolMode = ToolMode.Tile;

    public int CurrentLevel = 0;

    // 타일/벽/기둥 설정
    public FloorType SelectedFloorType = FloorType.Standard;
    public PillarType SelectedPillarType = PillarType.Standing;
    public EdgeType SelectedEdgeType = EdgeType.Wall;

    // [New] 포탈 모드 전용 상태값
    public PortalType SelectedPortalType = PortalType.In;
    public string CurrentPortalID = "Gate_1";
    public Direction CurrentPortalFacing = Direction.North;

    // [Change] 스폰 모드 전용 상태값 (MarkerType -> SpawnType)
    // 이제 여기서 Player인지 Enemy인지 선택합니다.
    public SpawnType SelectedSpawnType = SpawnType.Player;
    public string CurrentSpawnRoleTag = "Spawn_1";

    // 인터랙션 상태
    public GridCoords MouseGridCoords;
    public bool IsMouseOverGrid;
    public HighlightedEdgeInfo HighlightedEdge = new HighlightedEdgeInfo();

    // --- 캐싱 시스템 (기존 유지) ---
    private Dictionary<GridCoords, EditorTile> _tileCache = new Dictionary<GridCoords, EditorTile>();
    private Dictionary<(GridCoords, Direction), EditorWall> _wallCache = new Dictionary<(GridCoords, Direction), EditorWall>();
    private bool _isCacheDirty = true;

    public void InvalidateCache() => _isCacheDirty = true;

    public void RefreshCache()
    {
        if (_isCacheDirty) RebuildCache();
    }

    private void RebuildCache()
    {
        _tileCache.Clear();
        _wallCache.Clear();

        var tiles = FindObjectsOfType<EditorTile>();
        foreach (var t in tiles)
        {
            if (!_tileCache.ContainsKey(t.Coordinate)) _tileCache.Add(t.Coordinate, t);
        }

        var walls = FindObjectsOfType<EditorWall>();
        foreach (var w in walls)
        {
            if (!_wallCache.ContainsKey((w.Coordinate, w.Direction))) _wallCache.Add((w.Coordinate, w.Direction), w);
        }

        _isCacheDirty = false;
    }

    public EditorTile GetTile(GridCoords coords)
    {
        if (_isCacheDirty) RebuildCache();
        return _tileCache.TryGetValue(coords, out var tile) ? tile : null;
    }

    public EditorWall GetWall(GridCoords coords, Direction dir)
    {
        if (_isCacheDirty) RebuildCache();
        return _wallCache.TryGetValue((coords, dir), out var wall) ? wall : null;
    }

    public class HighlightedEdgeInfo
    {
        public GridCoords Tile;
        public Direction Dir;
        public Vector3 WorldPos;
        public bool IsValid;

        public void Set(GridCoords t, Direction d, Vector3 p, bool v)
        {
            Tile = t;
            Dir = d;
            WorldPos = p;
            IsValid = v;
        }
        public void SetInvalid() => IsValid = false;
    }
}