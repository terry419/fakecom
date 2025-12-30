// 파일 경로: Assets/Scripts/Editor/Modules/MapEditorIO.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

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
        // 1. 타겟 데이터가 없으면 새로 생성 (Save As)
        if (_context.TargetMapData == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Map Data", "NewMapData", "asset", "Save Map Data SO");
            if (string.IsNullOrEmpty(path)) return;

            MapDataSO newData = ScriptableObject.CreateInstance<MapDataSO>();
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();

            _context.TargetMapData = newData;
            Debug.Log($"[MapEditorIO] New MapDataSO created at: {path}");
        }

        // 2. 캐시 갱신 및 Undo 등록
        _context.RefreshCache();
        Undo.RecordObject(_context.TargetMapData, "Save Map Data");

        // [Fix] 실제 저장 로직 구현
        // 기존 데이터를 비우고 씬 데이터를 채워넣습니다.
        _context.TargetMapData.Tiles.Clear();

        // 씬의 모든 EditorTile을 찾습니다.
        EditorTile[] allTiles = Object.FindObjectsOfType<EditorTile>();

        foreach (var editorTile in allTiles)
        {
            TileSaveData data = new TileSaveData();
            data.Coords = editorTile.Coordinate;
            data.FloorID = editorTile.FloorID;
            data.PillarID = editorTile.PillarID;

            // 엣지 데이터 저장
            data.InitializeEdges();
            if (editorTile.Edges != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    // 구조체 값이므로 그대로 복사
                    data.Edges[i] = editorTile.Edges[i];
                }
            }

            _context.TargetMapData.Tiles.Add(data);
        }

        // 필요한 경우 맵의 메타데이터(Min/Max Level 등)도 갱신할 수 있습니다.
        // 현재는 Tiles 리스트만 업데이트합니다.

        Debug.Log($"[MapEditorIO] Map Saved to {_context.TargetMapData.name} (Total Tiles: {allTiles.Length})");

        // 변경 사항을 디스크에 기록
        EditorUtility.SetDirty(_context.TargetMapData);
        AssetDatabase.SaveAssets();
    }
}