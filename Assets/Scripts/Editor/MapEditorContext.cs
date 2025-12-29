using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// [Data] 공유 컨텍스트 & 캐시 저장소
public class MapEditorContext
{
    // --- 1. 설정 및 데이터 ---
    public MapEditorSettingsSO Settings;
    public MapDataSO TargetMapData;

    // --- 2. 작업 상태 ---
    public int CurrentLevel = 0;

    // [Fix] Pillar 모드 추가
    public enum ToolMode { Tile, Edge, Pillar, Erase }
    public ToolMode CurrentToolMode = ToolMode.Tile;

    public EdgeType SelectedEdgeType = EdgeType.Wall;
    public EdgeDataType SelectedEdgeDataType = EdgeDataType.Concrete;

    // [Fix] 선택된 기둥 타입
    public PillarType SelectedPillarType = PillarType.Concrete;

    // --- 3. 씬 뷰 상태 ---
    public GridCoords MouseGridCoords;
    public bool IsMouseOverGrid;
    public HighlightedEdgeInfo HighlightedEdge = new HighlightedEdgeInfo();

    // --- 4. 객체 캐싱 ---
    private Dictionary<GridCoords, EditorTile> _tileCache = new Dictionary<GridCoords, EditorTile>();
    private Dictionary<(GridCoords, Direction), EditorWall> _wallCache = new Dictionary<(GridCoords, Direction), EditorWall>();
    private bool _cacheValid = false;

    public void InvalidateCache() => _cacheValid = false;

    public void RefreshCache()
    {
        if (_cacheValid) return;

        _tileCache.Clear();
        _wallCache.Clear();

        var tiles = Object.FindObjectsOfType<EditorTile>();
        var walls = Object.FindObjectsOfType<EditorWall>();

        foreach (var tile in tiles)
        {
            if (!_tileCache.ContainsKey(tile.Coordinate))
                _tileCache.Add(tile.Coordinate, tile);
        }

        foreach (var wall in walls)
        {
            var key = (wall.Coordinate, wall.Direction);
            if (!_wallCache.ContainsKey(key))
                _wallCache.Add(key, wall);
        }

        _cacheValid = true;
    }

    public EditorTile GetTile(GridCoords coords)
    {
        RefreshCache();
        return _tileCache.TryGetValue(coords, out var tile) ? tile : null;
    }

    public EditorWall GetWall(GridCoords coords, Direction dir)
    {
        RefreshCache();
        return _wallCache.TryGetValue((coords, dir), out var wall) ? wall : null;
    }
}

// Helper Class for Highlights
public class HighlightedEdgeInfo
{
    public GridCoords Tile { get; set; }
    public Direction Dir { get; set; }
    public Vector3 WorldPos { get; set; }
    public bool IsValid { get; set; }

    public void Set(GridCoords tile, Direction dir, Vector3 pos, bool valid)
    {
        Tile = tile; Dir = dir; WorldPos = pos; IsValid = valid;
    }
    public void SetInvalid() { IsValid = false; }
}