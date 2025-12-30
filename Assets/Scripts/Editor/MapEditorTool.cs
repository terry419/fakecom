// 파일: Assets/Scripts/Editor/MapEditorTool.cs
using UnityEditor;
using UnityEngine;

public class MapEditorTool : EditorWindow
{
    // ... (기존 변수 및 초기화 코드는 동일하게 유지) ...
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

        _sceneInput.OnCreateTileRequested += _action.HandleCreateTile;
        _sceneInput.OnModifyEdgeRequested += _action.HandleModifyEdge;
        _sceneInput.OnCreatePillarRequested += _action.HandleCreatePillar;
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

    // 파일: Assets/Scripts/Editor/MapEditorTool.cs

    private void OnGUI()
    {
        GUILayout.Label("Construction Settings", EditorStyles.boldLabel);
        _context.Settings = (MapEditorSettingsSO)EditorGUILayout.ObjectField("Settings", _context.Settings, typeof(MapEditorSettingsSO), false);

        // [Fix] MapData의 MaxLevel 설정이 0이면 슬라이더가 고정되므로, 
        // 맵 데이터의 층수 설정을 툴에서 바로 변경할 수 있게 합니다.
        int maxLevel = 5;
        if (_context.TargetMapData != null)
        {
            // MapDataSO의 값을 직접 수정하도록 연결
            Undo.RecordObject(_context.TargetMapData, "Change Max Level");
            _context.TargetMapData.MaxLevel = EditorGUILayout.IntField("Map Max Level", _context.TargetMapData.MaxLevel);
            maxLevel = _context.TargetMapData.MaxLevel;
        }

        // 레벨 슬라이더
        EditorGUI.BeginChangeCheck();
        _context.CurrentLevel = EditorGUILayout.IntSlider("Current Level (Y)", _context.CurrentLevel, 0, maxLevel);
        if (EditorGUI.EndChangeCheck())
        {
            SceneView.RepaintAll();
        }

        EditorGUILayout.Space();

        // 툴 모드 선택 UI
        _context.CurrentToolMode = (MapEditorContext.ToolMode)EditorGUILayout.EnumPopup("Mode", _context.CurrentToolMode);

        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge)
        {
            _context.SelectedEdgeType = (EdgeType)EditorGUILayout.EnumPopup("Edge Type", _context.SelectedEdgeType);
            _context.SelectedEdgeDataType = (EdgeDataType)EditorGUILayout.EnumPopup("Edge Material", _context.SelectedEdgeDataType);
        }
        else if (_context.CurrentToolMode == MapEditorContext.ToolMode.Pillar)
        {
            _context.SelectedPillarType = (PillarType)EditorGUILayout.EnumPopup("Pillar Type", _context.SelectedPillarType);
        }

        EditorGUILayout.Space();
        GUILayout.Label("Map Data IO", EditorStyles.boldLabel);

        // Map Data 필드
        EditorGUI.BeginChangeCheck();
        _context.TargetMapData = (MapDataSO)EditorGUILayout.ObjectField("Target Map Data", _context.TargetMapData, typeof(MapDataSO), false);
        if (EditorGUI.EndChangeCheck())
        {
            // 데이터 교체 시 즉시 MaxLevel 반영을 위해 리페인트
            Repaint();
        }

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Load Data to Scene"))
        {
            // [수정] TargetMapData와 Settings가 모두 할당되었는지 확인
            if (_context.TargetMapData != null && _context.Settings != null)
            {
                _action.LoadMapFromData(_context.TargetMapData);
            }
            else
            {
                // 더 상세한 에러 메시지 제공
                if (_context.TargetMapData == null)
                    Debug.LogError("Target Map Data가 비어있습니다! 로드할 데이터를 먼저 할당하세요.");
                if (_context.Settings == null)
                    Debug.LogError("Settings가 비어있습니다! Map Editor Settings 에셋을 할당하세요.");
            }
        }

        if (GUILayout.Button("Save Scene to Data"))
        {
            // [수정] TargetMapData와 Settings가 모두 할당되었는지 확인
            if (_context.TargetMapData != null && _context.Settings != null)
            {
                _io.SaveMap();
            }
            else
            {
                // 더 상세한 에러 메시지 제공
                if (_context.TargetMapData == null)
                    Debug.LogError("Target Map Data가 비어있습니다! 저장할 데이터를 먼저 할당하세요.");
                if (_context.Settings == null)
                    Debug.LogError("Settings가 비어있습니다! Map Editor Settings 에셋을 할당하세요.");
            }
        }
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        _sceneInput?.HandleSceneGUI(sceneView);
        sceneView.Repaint();
    }
}