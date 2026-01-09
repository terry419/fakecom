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
        // 1. 타겟 데이터 생성
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

        // 2. 맵 요소 수집
        var allMarkers = Object.FindObjectsOfType<EditorMarker>();
        EditorTile[] allTiles = Object.FindObjectsOfType<EditorTile>();

        if (allTiles.Length == 0) return;

        // 3. 맵 경계 계산
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
            data.CurrentPillarHP = 0f;

            // 마커 매칭
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

                    // =================================================================================
                    // [핵심 Fix] 포탈 연결 로직 (Link Processing)
                    // 같은 ID를 가진 다른 포탈(Out/Both)을 찾아서 목적지로 등록합니다.
                    // =================================================================================
                    if (marker.PType == PortalType.In || marker.PType == PortalType.Both)
                    {
                        // 1. 나(marker)와 ID가 같고, 내가 아닌 다른 마커들 찾기
                        var targets = allMarkers.Where(m =>
                            m.MarkerCategory == MarkerType.Portal &&
                            m.ID == marker.ID &&
                            m != marker).ToList();

                        foreach (var target in targets)
                        {
                            // 2. 목적지가 될 수 있는 타입인지 확인 (Out or Both)
                            if (target.PType == PortalType.Out || target.PType == PortalType.Both)
                            {
                                GridCoords targetCoords = GridUtils.WorldToGrid(target.transform.position);

                                // 3. Destinations 리스트에 추가
                                data.PortalData.Destinations.Add(new PortalDestination(targetCoords, target.Facing));
                                Debug.Log($"[Link] Portal Linked: {data.Coords} -> {targetCoords} (ID: {marker.ID})");
                            }
                        }
                    }
                }
            }
            else
            {
                data.RoleTag = "";
                data.PortalData = null;
            }

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