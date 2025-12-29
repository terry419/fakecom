using UnityEngine;
using UnityEditor;

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

    // [Fix] MapDataSO가 없으면 생성해서 저장
    public void SaveMap()
    {
        // 1. 타겟 데이터가 없으면 새로 생성 (Save As)
        if (_context.TargetMapData == null)
        {
            string path = EditorUtility.SaveFilePanelInProject("Create New Map Data", "NewMapData", "asset", "Save Map Data SO");
            if (string.IsNullOrEmpty(path)) return; // 취소함

            MapDataSO newData = ScriptableObject.CreateInstance<MapDataSO>();
            AssetDatabase.CreateAsset(newData, path);
            AssetDatabase.SaveAssets();

            _context.TargetMapData = newData;
            Debug.Log($"[MapEditorIO] New MapDataSO created at: {path}");
        }

        // 2. 저장 로직 진행
        _context.RefreshCache();
        Undo.RecordObject(_context.TargetMapData, "Save Map Data");

        // ... (타일 데이터 수집 및 SO 저장 로직) ...
        // 실제 구현 시 Action 모듈 없이 여기서 씬 객체(EditorTile)들을 긁어모아 저장하면 됩니다.
        Debug.Log($"Map Saved to {_context.TargetMapData.name}");
        EditorUtility.SetDirty(_context.TargetMapData);
        AssetDatabase.SaveAssets();
    }
}