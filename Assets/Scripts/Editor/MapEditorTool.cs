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
        // 1. Context 초기화 (Singleton)
        _context = MapEditorContext.Instance;

        // 2. 모듈 인스턴스 생성
        _sceneInput = new MapEditorSceneInput(_context);
        _action = new MapEditorAction(_context);
        _io = new MapEditorIO(_context);

        // 3. 이벤트 연결 (사용자 입력 -> 액션 실행)
        _sceneInput.OnCreateTileRequested += _action.HandleCreateTile;
        _sceneInput.OnModifyEdgeRequested += _action.HandleModifyEdge;
        _sceneInput.OnCreatePillarRequested += _action.HandleCreatePillar;
        _sceneInput.OnEraseTileRequested += _action.HandleEraseTile;

        // 4. 씬 GUI 델리게이트 등록
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;

        // 이벤트 연결 해제 (메모리 누수 방지)
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
        // 플레이 모드 중에는 에디터 GUI 비활성화
        if (Application.isPlaying)
        {
            GUILayout.Label("Editor disabled during Play Mode", EditorStyles.boldLabel);
            return;
        }

        if (_context == null) _context = MapEditorContext.Instance;

        GUILayout.Label("Construction Settings", EditorStyles.boldLabel);
        _context.Settings = (MapEditorSettingsSO)EditorGUILayout.ObjectField("Settings", _context.Settings, typeof(MapEditorSettingsSO), false);

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

        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge)
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
            if (_context.TargetMapData != null && _context.Settings != null)
                _action.LoadMapFromData(_context.TargetMapData);
            else
                Debug.LogError("TargetMapData or Settings is missing!");
        }

        if (GUILayout.Button("Save Scene to Data"))
        {
            if (_context.TargetMapData != null && _context.Settings != null)
                _io.SaveMap();
            else
                Debug.LogError("TargetMapData or Settings is missing!");
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