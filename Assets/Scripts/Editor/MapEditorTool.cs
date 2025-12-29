using UnityEditor;
using UnityEngine;

public class MapEditorTool : EditorWindow
{
    private MapEditorContext _context;
    private MapEditorSceneInput _sceneInput;
    private MapEditorAction _action;
    private MapEditorIO _io;

    [MenuItem("YCOM/Map Editor Tool")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorTool>("Map Editor");
    }

    private void OnEnable()
    {
        if (_context == null) _context = new MapEditorContext();

        _sceneInput = new MapEditorSceneInput(_context);
        _action = new MapEditorAction(_context);
        _io = new MapEditorIO(_context);

        // 이벤트 연결
        _sceneInput.OnCreateTileRequested += _action.HandleCreateTile;
        _sceneInput.OnModifyEdgeRequested += _action.HandleModifyEdge;
        _sceneInput.OnCreatePillarRequested += _action.HandleCreatePillar; // [Fix] 연결
        _sceneInput.OnEraseTileRequested += _action.HandleEraseTile;

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (_sceneInput != null)
        {
            _sceneInput.OnCreateTileRequested -= _action.HandleCreateTile;
            _sceneInput.OnModifyEdgeRequested -= _action.HandleModifyEdge;
            _sceneInput.OnCreatePillarRequested -= _action.HandleCreatePillar;
            _sceneInput.OnEraseTileRequested -= _action.HandleEraseTile;
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Construction Settings", EditorStyles.boldLabel);

        _context.Settings = (MapEditorSettingsSO)EditorGUILayout.ObjectField("Settings", _context.Settings, typeof(MapEditorSettingsSO), false);

        // [Fix] ToolMode UI
        _context.CurrentToolMode = (MapEditorContext.ToolMode)EditorGUILayout.EnumPopup("Mode", _context.CurrentToolMode);

        // Edge Mode UI
        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge)
        {
            _context.SelectedEdgeType = (EdgeType)EditorGUILayout.EnumPopup("Edge Type", _context.SelectedEdgeType);
            _context.SelectedEdgeDataType = (EdgeDataType)EditorGUILayout.EnumPopup("Edge Material", _context.SelectedEdgeDataType);
        }
        // [Fix] Pillar Mode UI
        else if (_context.CurrentToolMode == MapEditorContext.ToolMode.Pillar)
        {
            _context.SelectedPillarType = (PillarType)EditorGUILayout.EnumPopup("Pillar Type", _context.SelectedPillarType);
        }

        EditorGUILayout.Space();
        GUILayout.Label("File IO", EditorStyles.boldLabel);

        _context.TargetMapData = (MapDataSO)EditorGUILayout.ObjectField("Map Data", _context.TargetMapData, typeof(MapDataSO), false);

        // Load / Save Buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Map"))
        {
            MapDataSO data = _io.LoadMapData();
            if (data != null) _action.LoadMapFromData(data);
        }

        // [Fix] Save Map 버튼 (파일 없으면 생성됨)
        if (GUILayout.Button("Save Map"))
        {
            _io.SaveMap();
        }
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        _sceneInput?.HandleSceneGUI(sceneView);
        sceneView.Repaint();
    }
}