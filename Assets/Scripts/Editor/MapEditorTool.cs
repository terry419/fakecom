using UnityEngine;
using UnityEditor;

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
        _context = MapEditorContext.Instance;
        _sceneInput = new MapEditorSceneInput(_context);
        _action = new MapEditorAction(_context);
        _io = new MapEditorIO(_context);

        _sceneInput.OnCreateTileRequested += _action.HandleCreateTile;
        _sceneInput.OnModifyEdgeRequested += _action.HandleModifyEdge;
        _sceneInput.OnCreatePillarRequested += _action.HandleCreatePillar;
        _sceneInput.OnEraseTileRequested += _action.HandleEraseTile;

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        if (_sceneInput != null && _action != null)
        {
            _sceneInput.OnCreateTileRequested -= _action.HandleCreateTile;
            _sceneInput.OnModifyEdgeRequested -= _action.HandleModifyEdge;
            _sceneInput.OnCreatePillarRequested -= _action.HandleCreatePillar;
            _sceneInput.OnEraseTileRequested -= _action.HandleEraseTile;
        }
    }

    private void OnGUI()
    {
        if (Application.isPlaying)
        {
            GUILayout.Label("Editor disabled during Play Mode", EditorStyles.boldLabel);
            return;
        }

        if (_context == null) _context = MapEditorContext.Instance;

        GUILayout.Label("Construction Settings", EditorStyles.boldLabel);

        // [Wiring] Registry만 남기고 Settings 삭제
        _context.Registry = (TileRegistrySO)EditorGUILayout.ObjectField("Tile Registry", _context.Registry, typeof(TileRegistrySO), false);

        if (_context.Registry == null)
        {
            EditorGUILayout.HelpBox("Please assign Tile Registry!", MessageType.Error);
        }

        // Max Level 설정
        int maxLevel = 5;
        if (_context.TargetMapData != null)
        {
            Undo.RecordObject(_context.TargetMapData, "Change Max Level");
            _context.TargetMapData.MaxLevel = EditorGUILayout.IntField("Map Max Level", _context.TargetMapData.MaxLevel);
            maxLevel = _context.TargetMapData.MaxLevel;
        }

        // Current Level 슬라이더
        EditorGUI.BeginChangeCheck();
        _context.CurrentLevel = EditorGUILayout.IntSlider("Current Level (Y)", _context.CurrentLevel, 0, maxLevel);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();

        // 툴 모드 선택
        _context.CurrentToolMode = (MapEditorContext.ToolMode)EditorGUILayout.EnumPopup("Mode", _context.CurrentToolMode);

        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Tile)
        {
            _context.SelectedFloorType = (FloorType)EditorGUILayout.EnumPopup("Floor Type", _context.SelectedFloorType);
        }
        else if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge)
        {
            _context.SelectedEdgeType = (EdgeType)EditorGUILayout.EnumPopup("Edge Type", _context.SelectedEdgeType);
        }
        else if (_context.CurrentToolMode == MapEditorContext.ToolMode.Pillar)
        {
            _context.SelectedPillarType = (PillarType)EditorGUILayout.EnumPopup("Pillar Type", _context.SelectedPillarType);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Map Data IO", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        _context.TargetMapData = (MapDataSO)EditorGUILayout.ObjectField("Target Map Data", _context.TargetMapData, typeof(MapDataSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            Repaint();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Data to Scene"))
        {
            if (_context.TargetMapData != null && _context.Registry != null)
                _action.LoadMapFromData(_context.TargetMapData);
            else
                Debug.LogError("TargetMapData or Registry is missing!");
        }

        if (GUILayout.Button("Save Scene to Data"))
        {
            if (_context.TargetMapData != null)
                _io.SaveMap();
            else
                Debug.LogError("TargetMapData is missing!");
        }
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying) return;
        _sceneInput?.HandleSceneGUI(sceneView);
        sceneView.Repaint();
    }
}