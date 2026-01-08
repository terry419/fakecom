using UnityEngine;
using UnityEditor;

public class MapEditorTool : EditorWindow
{
    private MapEditorContext _context;
    private MapEditorSceneInput _sceneInput;
    private MapEditorAction _action;
    private MapEditorIO _io;

    [MenuItem("YCOM/Map Editor Tool")]
    public static void ShowWindow() { GetWindow<MapEditorTool>("Map Editor"); }

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

        _sceneInput.OnCreatePortalRequested += OnRequestCreatePortal;
        _sceneInput.OnCreateSpawnRequested += OnRequestCreateSpawn;
        _sceneInput.OnRemoveMarkerRequested += _action.HandleRemoveMarker;

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
            _sceneInput.OnCreatePortalRequested -= OnRequestCreatePortal;
            _sceneInput.OnCreateSpawnRequested -= OnRequestCreateSpawn;
            _sceneInput.OnRemoveMarkerRequested -= _action.HandleRemoveMarker;
        }
    }

    private void OnRequestCreatePortal(GridCoords coords)
    {
        _action.HandleCreatePortal(coords, _context.SelectedPortalType, _context.CurrentPortalID, _context.CurrentPortalFacing);
    }

    private void OnRequestCreateSpawn(GridCoords coords)
    {
        _action.HandleCreateSpawn(coords, _context.SelectedSpawnType, _context.CurrentSpawnRoleTag);
    }

    private void OnGUI()
    {
        if (Application.isPlaying) { GUILayout.Label("Editor disabled during Play Mode"); return; }
        if (_context == null) _context = MapEditorContext.Instance;

        DrawCommonSettings();
        GUILayout.Space(10);

        _context.CurrentToolMode = (MapEditorContext.ToolMode)EditorGUILayout.EnumPopup("Editor Mode", _context.CurrentToolMode);
        GUILayout.Space(5);

        switch (_context.CurrentToolMode)
        {
            case MapEditorContext.ToolMode.Tile: DrawTileUI(); break;
            case MapEditorContext.ToolMode.Edge: DrawEdgeUI(); break;
            case MapEditorContext.ToolMode.Pillar: DrawPillarUI(); break;
            case MapEditorContext.ToolMode.Portal: DrawPortalUI(); break;
            case MapEditorContext.ToolMode.Spawn: DrawSpawnUI(); break;
            case MapEditorContext.ToolMode.Erase:
                EditorGUILayout.HelpBox("Click to erase Tile & Marker.", MessageType.Info);
                break;
        }

        GUILayout.FlexibleSpace();
        DrawIOSettings();
    }

    private void DrawCommonSettings()
    {
        GUILayout.Label("Global Settings", EditorStyles.boldLabel);
        _context.Registry = (TileRegistrySO)EditorGUILayout.ObjectField("Tile Registry", _context.Registry, typeof(TileRegistrySO), false);
        if (_context.Registry == null) EditorGUILayout.HelpBox("Registry Missing!", MessageType.Error);

        int maxLevel = (_context.TargetMapData != null) ? _context.TargetMapData.MaxLevel : 5;
        _context.CurrentLevel = EditorGUILayout.IntSlider("Current Level (Y)", _context.CurrentLevel, 0, maxLevel);
    }

    private void DrawTileUI() { _context.SelectedFloorType = (FloorType)EditorGUILayout.EnumPopup("Floor Type", _context.SelectedFloorType); }
    private void DrawEdgeUI() { _context.SelectedEdgeType = (EdgeType)EditorGUILayout.EnumPopup("Edge Type", _context.SelectedEdgeType); }
    private void DrawPillarUI() { _context.SelectedPillarType = (PillarType)EditorGUILayout.EnumPopup("Pillar Type", _context.SelectedPillarType); }

    private void DrawPortalUI()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Portal Settings", EditorStyles.boldLabel);
        _context.SelectedPortalType = (PortalType)EditorGUILayout.EnumPopup("Portal Type", _context.SelectedPortalType);
        _context.CurrentPortalID = EditorGUILayout.TextField("Link ID", _context.CurrentPortalID);
        if (_context.SelectedPortalType == PortalType.Out)
            _context.CurrentPortalFacing = (Direction)EditorGUILayout.EnumPopup("Exit Facing", _context.CurrentPortalFacing);
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click: Create Portal\nAlt+Click: Remove", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private void DrawSpawnUI()
    {
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Spawn Settings", EditorStyles.boldLabel);

        _context.SelectedSpawnType = (MarkerType)EditorGUILayout.EnumPopup("Spawn Type", _context.SelectedSpawnType);

        // [Fix] CS0117 오류 해결: MarkerType.Portal_In 등은 삭제되었으므로, 
        // MarkerType이 Portal인 경우 강제로 PlayerSpawn으로 돌려놓는 안전장치 수정
        if (_context.SelectedSpawnType == MarkerType.Portal)
        {
            _context.SelectedSpawnType = MarkerType.PlayerSpawn;
        }

        _context.CurrentSpawnRoleTag = EditorGUILayout.TextField("Role Tag", _context.CurrentSpawnRoleTag);

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Click: Create Spawn\nAlt+Click: Remove", MessageType.Info);
        EditorGUILayout.EndVertical();
    }

    private void DrawIOSettings()
    {
        GUILayout.Label("Map Data IO", EditorStyles.boldLabel);
        _context.TargetMapData = (MapDataSO)EditorGUILayout.ObjectField("Target Map Data", _context.TargetMapData, typeof(MapDataSO), false);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Load Data")) _action.LoadMapFromData(_context.TargetMapData);
        if (GUILayout.Button("Save Data")) _io.SaveMap();
        GUILayout.EndHorizontal();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (Application.isPlaying) return;
        _sceneInput?.HandleSceneGUI(sceneView);
        sceneView.Repaint();
    }
}