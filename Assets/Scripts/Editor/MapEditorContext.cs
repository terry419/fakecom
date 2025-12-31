using UnityEngine;
using System.Collections.Generic;

public class MapEditorContext : ScriptableObject
{
    private static MapEditorContext _instance;
    public static MapEditorContext Instance => _instance ??= CreateInstance<MapEditorContext>();

    // [Wiring] Settings 삭제 -> Registry가 유일한 장부
    public TileRegistrySO Registry;
    public MapDataSO TargetMapData;

    public enum ToolMode { Tile, Edge, Pillar, Erase }
    public ToolMode CurrentToolMode = ToolMode.Tile;

    public int CurrentLevel = 0;

    public FloorType SelectedFloorType = FloorType.Standard;
    public PillarType SelectedPillarType = PillarType.Standing;
    public EdgeType SelectedEdgeType = EdgeType.Wall;

    public GridCoords MouseGridCoords;
    public bool IsMouseOverGrid;
    public HighlightedEdgeInfo HighlightedEdge = new HighlightedEdgeInfo();

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
            if (!_tileCache.ContainsKey(t.Coordinate))
                _tileCache.Add(t.Coordinate, t);
        }

        var walls = FindObjectsOfType<EditorWall>();
        foreach (var w in walls)
        {
            if (!_wallCache.ContainsKey((w.Coordinate, w.Direction)))
                _wallCache.Add((w.Coordinate, w.Direction), w);
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
            Dir = d; WorldPos = p; IsValid = v;
        }
        public void SetInvalid() => IsValid = false;
    }
}