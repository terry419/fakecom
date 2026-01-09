using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class MapEditorIO
{
    private readonly MapEditorContext _context;

    public MapEditorIO(MapEditorContext context)
    {
        _context = context;
    }

    public MapDataSO LoadMapData()
    {
        string path = EditorUtility.OpenFilePanel("Load Map Data", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return null;

        path = "Assets" + path.Substring(Application.dataPath.Length);
        MapDataSO loadedData = AssetDatabase.LoadAssetAtPath<MapDataSO>(path);

        if (loadedData != null) _context.TargetMapData = loadedData;
        return loadedData;
    }

    public void SaveMap()
    {
        // 1. 타겟 데이터 생성 또는 확보
        if (_context.TargetMapData == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Map Data", "NewMapData", "asset", "Save Map Data SO");
            if (string.IsNullOrEmpty(path)) return;

            MapDataSO newData = ScriptableObject.CreateInstance<MapDataSO>();
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();

            _context.TargetMapData = newData;
        }

        _context.RefreshCache();
        Undo.RecordObject(_context.TargetMapData, "Save Map Data");

        _context.TargetMapData.Tiles.Clear();

        // 2. 맵에 존재하는 모든 요소 수집
        var allMarkers = Object.FindObjectsOfType<EditorMarker>();
        EditorTile[] allTiles = Object.FindObjectsOfType<EditorTile>();

        if (allTiles.Length == 0)
        {
            Debug.LogWarning("[MapEditorIO] 저장할 타일이 없습니다.");
            return;
        }

        // 3. [핵심] 맵의 경계(Grid Size) 자동 계산
        // 이 부분이 있어야 MapManager가 타일 배열을 올바른 크기로 생성합니다.
        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var t in allTiles)
        {
            if (t.Coordinate.x < minX) minX = t.Coordinate.x;
            if (t.Coordinate.x > maxX) maxX = t.Coordinate.x;

            if (t.Coordinate.z < minZ) minZ = t.Coordinate.z;
            if (t.Coordinate.z > maxZ) maxZ = t.Coordinate.z;

            if (t.Coordinate.y < minY) minY = t.Coordinate.y;
            if (t.Coordinate.y > maxY) maxY = t.Coordinate.y;
        }

        // 계산된 크기 적용
        _context.TargetMapData.GridSize = new Vector2Int(maxX - minX + 1, maxZ - minZ + 1);
        _context.TargetMapData.BasePosition = new Vector2Int(minX, minZ);
        _context.TargetMapData.MinLevel = minY;
        _context.TargetMapData.MaxLevel = maxY;

        Debug.Log($"[MapEditorIO] Bounds Calculated: Size={_context.TargetMapData.GridSize}, Base={_context.TargetMapData.BasePosition}");

        // 4. 데이터 저장 루프
        foreach (var editorTile in allTiles)
        {
            TileSaveData data = new TileSaveData();
            data.Coords = editorTile.Coordinate;
            data.FloorID = editorTile.FloorID;
            data.PillarID = editorTile.PillarID;

            // [Fix] CS1061 오류 수정
            // EditorTile에는 현재 체력 정보가 없으므로 0으로 저장합니다.
            // (런타임 로드 시 0이면 자동으로 MaxHP로 초기화됩니다)
            data.CurrentPillarHP = 0f;

            // 마커 정보 매칭 (Spawn, Portal)
            EditorMarker marker = allMarkers.FirstOrDefault(m =>
                IsCoordinateMatch(m.transform.position, editorTile.Coordinate));

            if (marker != null)
            {
                if (marker.MarkerCategory == MarkerType.Spawn && !string.IsNullOrEmpty(marker.ID))
                {
                    data.RoleTag = marker.ID;
                    data.SpawnType = marker.SType;
                }
                else if (marker.MarkerCategory == MarkerType.Portal && !string.IsNullOrEmpty(marker.ID))
                {
                    data.PortalData = new PortalInfo
                    {
                        Type = marker.PType,
                        LinkID = marker.ID,
                        ExitFacing = marker.Facing
                    };
                }
            }
            else
            {
                data.RoleTag = "";
                data.PortalData = null;
            }

            // 엣지 정보 저장
            data.InitializeEdges();
            if (editorTile.Edges != null)
            {
                for (int i = 0; i < 4; i++) data.Edges[i] = editorTile.Edges[i];
            }

            _context.TargetMapData.Tiles.Add(data);
        }

        EditorUtility.SetDirty(_context.TargetMapData);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MapEditorIO] 맵 저장 완료: {allTiles.Length} tiles saved.");
    }

    private bool IsCoordinateMatch(Vector3 markerPos, GridCoords tileCoords)
    {
        GridCoords markerGrid = GridUtils.WorldToGrid(markerPos);
        return markerGrid.x == tileCoords.x &&
               markerGrid.y == tileCoords.y &&
               markerGrid.z == tileCoords.z;
    }
}