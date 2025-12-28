using UnityEditor;
using UnityEngine;
using UnityEditor.EditorTools;
using System.Collections.Generic;
using static UnityEditor.Handles;

public class MapEditorTool : EditorWindow
{
    private int currentLevel = 0;
    private MapEditorSettingsSO settings;
    private MapDataSO targetMapData;
    private GridCoords mouseGridCoords;
    private bool mouseOverGrid;
    private Transform tileParent;
    private EdgeType selectedEdgeType = EdgeType.Wall;
    private EdgeDataType selectedEdgeDataType = EdgeDataType.Concrete;

    private (GridCoords tileCoords, Direction edgeDir, Vector3 worldPos, bool isValid) highlightedEdge;

    [MenuItem("YCOM/Map Editor Tool")]
    public static void ShowWindow()
    {
        GetWindow<MapEditorTool>("Map Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
        FindOrCreateTileParent();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Construction Settings", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        currentLevel = EditorGUILayout.IntField("Current Level (Y)", currentLevel);
        if (GUILayout.Button("-", GUILayout.Width(30))) currentLevel--;
        if (GUILayout.Button("+", GUILayout.Width(30))) currentLevel++;
        GUILayout.EndHorizontal();
        if (currentLevel < 0) currentLevel = 0;

        EditorGUILayout.Space();
        settings = (MapEditorSettingsSO)EditorGUILayout.ObjectField("Settings Profile", settings, typeof(MapEditorSettingsSO), false);

        if (settings == null)
        {
            EditorGUILayout.HelpBox("먼저 'MapEditorSettings' 에셋을 생성하고 할당해주세요!", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        GUILayout.Label("Bake/Load Settings", EditorStyles.boldLabel);
        targetMapData = (MapDataSO)EditorGUILayout.ObjectField("Target Map Data", targetMapData, typeof(MapDataSO), false);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save Map to SO")) SaveMap();
        if (GUILayout.Button("Load Map from SO")) LoadMap();
        GUILayout.EndHorizontal();

        EditorGUILayout.Space();
        GUILayout.Label("Edge Construction", EditorStyles.boldLabel);
        selectedEdgeType = (EdgeType)EditorGUILayout.EnumPopup("Edge Type", selectedEdgeType);
        selectedEdgeDataType = (EdgeDataType)EditorGUILayout.EnumPopup("Edge Material", selectedEdgeDataType);

        EditorGUILayout.Space();
        if (mouseOverGrid) EditorGUILayout.LabelField("Mouse Grid:", mouseGridCoords.ToString());
        if (highlightedEdge.isValid) EditorGUILayout.LabelField("Highlighted Edge:", $"{highlightedEdge.tileCoords}-{highlightedEdge.edgeDir}");
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (settings == null) return;

        Event guiEvent = Event.current;
        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, currentLevel * GridUtils.LEVEL_HEIGHT, 0));

        if (gridPlane.Raycast(mouseRay, out float distance))
        {
            Vector3 worldPos = mouseRay.GetPoint(distance);
            mouseGridCoords = GridUtils.WorldToGrid(worldPos);
            mouseGridCoords.y = currentLevel;
            mouseOverGrid = true;

            Color tileHighlightColor = settings.GridHighlightMaterial != null ? settings.GridHighlightMaterial.color : Color.cyan;
            DrawTileHighlight(mouseGridCoords, tileHighlightColor);

            highlightedEdge = GetMouseOverEdge(worldPos);
            if (highlightedEdge.isValid)
            {
                Color edgeHighlightColor = settings.EdgeHighlightMaterial != null ? settings.EdgeHighlightMaterial.color : Color.magenta;
                DrawEdgeHighlight(highlightedEdge.worldPos, highlightedEdge.edgeDir, edgeHighlightColor);
            }

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                if (highlightedEdge.isValid)
                {
                    ModifyEdge(highlightedEdge);
                }
                else
                {
                    CreateTile(mouseGridCoords);
                }
                guiEvent.Use();
            }

            sceneView.Repaint();
            Repaint();
        }
        else
        {
            mouseOverGrid = false;
            highlightedEdge.isValid = false;
        }
    }

    private void DrawTileHighlight(GridCoords coords, Color color)
    {
        Color oldColor = UnityEditor.Handles.color;
        UnityEditor.Handles.color = color;
        DrawWireCube(GridUtils.GridToWorld(coords), new Vector3(GridUtils.CELL_SIZE, 0.1f, GridUtils.CELL_SIZE));
        UnityEditor.Handles.color = oldColor;
    }

    private void DrawEdgeHighlight(Vector3 center, Direction dir, Color color)
    {
        float thickness = GridUtils.CELL_SIZE * 0.1f;
        Vector3 size = (dir == Direction.North || dir == Direction.South)
            ? new Vector3(GridUtils.CELL_SIZE, 0.2f, thickness)
            : new Vector3(thickness, 0.2f, GridUtils.CELL_SIZE);

        Color oldColor = UnityEditor.Handles.color;
        UnityEditor.Handles.color = color;
        DrawWireCube(center, size);
        UnityEditor.Handles.color = oldColor;
    }

    private (GridCoords, Direction, Vector3, bool) GetMouseOverEdge(Vector3 mouseWorldPos)
    {
        GridCoords tileCoords = GridUtils.WorldToGrid(mouseWorldPos);
        tileCoords.y = currentLevel;
        Vector3 tileCenter = GridUtils.GridToWorld(tileCoords);
        Vector3 offset = mouseWorldPos - tileCenter;

        float threshold = GridUtils.CELL_SIZE * 0.35f;
        float absX = Mathf.Abs(offset.x);
        float absZ = Mathf.Abs(offset.z);

        if (absX < threshold && absZ < threshold)
            return (tileCoords, Direction.None, Vector3.zero, false);

        Direction dir = (absX > absZ)
            ? (offset.x > 0 ? Direction.East : Direction.West)
            : (offset.z > 0 ? Direction.North : Direction.South);

        Vector3 edgePos = GridUtils.GetEdgeWorldPosition(tileCoords, dir);
        return (tileCoords, dir, edgePos, true);
    }

    private void ModifyEdge((GridCoords tileCoords, Direction edgeDir, Vector3 worldPos, bool isValid) edge)
    {
        // First, find and destroy any existing visual wall object at this exact position.
        // The OnDestroy method of EditorWall will handle cleaning up the data on adjacent tiles.
        EditorWall[] allWalls = FindObjectsOfType<EditorWall>();
        foreach (var wall in allWalls)
        {
            var (neighborCoords, oppositeDir) = GridUtils.GetOppositeEdge(edge.tileCoords, edge.edgeDir);
            bool isTargetWall = (wall.Coordinate == edge.tileCoords && wall.Direction == edge.edgeDir) ||
                                (wall.Coordinate == neighborCoords && wall.Direction == oppositeDir);
            if (isTargetWall)
            {
                Undo.DestroyObjectImmediate(wall.gameObject);
            }
        }

        // If we're just erasing, we're done.
        if (selectedEdgeType == EdgeType.Open)
        {
            Debug.Log($"Erased edge at {edge.tileCoords}-{edge.edgeDir}");
            return;
        }

        // --- Create new wall ---
        GameObject prefabToUse = GetPrefabForEdgeType(selectedEdgeType);
        if (prefabToUse == null)
        {
            Debug.LogError($"[MapEditorTool] Prefab for {selectedEdgeType} is not set!");
            return;
        }

        Transform edgeParent = GetOrCreateEdgeParent();
        SavedEdgeInfo newEdgeData = CreateEdgeDataFromSelection();

        GameObject newWallObj = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, edgeParent);
        Undo.RegisterCreatedObjectUndo(newWallObj, "Create " + selectedEdgeType);

        newWallObj.transform.position = edge.worldPos + new Vector3(0, GridUtils.LEVEL_HEIGHT / 2.0f, 0);
        newWallObj.transform.rotation = (edge.edgeDir == Direction.North || edge.edgeDir == Direction.South)
                                         ? Quaternion.identity
                                         : Quaternion.Euler(0, 90, 0);

        var editorWall = newWallObj.GetComponent<EditorWall>();
        if (editorWall != null)
        {
            // Initializing the new wall will trigger its OnEnable/OnValidate,
            // which handles updating the neighboring tiles' data automatically.
            editorWall.Initialize(edge.tileCoords, edge.edgeDir, newEdgeData);
        }

        Debug.Log($"[MapEditorTool] Created {selectedEdgeType} at {edge.tileCoords}-{edge.edgeDir}");
    }

    private SavedEdgeInfo CreateEdgeDataFromSelection()
    {
        switch (selectedEdgeType)
        {
            case EdgeType.Wall:
                return SavedEdgeInfo.CreateWall(selectedEdgeDataType);
            case EdgeType.Window:
                return SavedEdgeInfo.CreateWindow(selectedEdgeDataType);
            case EdgeType.Door:
                return SavedEdgeInfo.CreateDoor(selectedEdgeDataType);
            default:
                return SavedEdgeInfo.CreateOpen();
        }
    }

    private GameObject GetPrefabForEdgeType(EdgeType type)
    {
        switch (type)
        {
            case EdgeType.Wall: return settings.DefaultWallPrefab;
            case EdgeType.Window: return settings.DefaultWindowPrefab;
            case EdgeType.Door: return settings.DefaultDoorPrefab;
            default: return null;
        }
    }

    private EditorTile GetEditorTileAt(GridCoords coords)
    {
        if (tileParent == null) return null;
        string tileName = $"Tile_{coords.x}_{coords.z}_{coords.y}";
        Transform tileTransform = tileParent.Find(tileName);
        return tileTransform?.GetComponent<EditorTile>();
    }

    private Transform GetOrCreateEdgeParent()
    {
        FindOrCreateTileParent();
        Transform edgeParent = tileParent.Find("--- EDGES ---");
        if (edgeParent == null)
        {
            var edgeParentObj = new GameObject("--- EDGES ---");
            edgeParentObj.transform.SetParent(tileParent);
            edgeParent = edgeParentObj.transform;
        }
        return edgeParent;
    }

    private void FindOrCreateTileParent()
    {
        if (tileParent == null)
        {
            GameObject parentObj = GameObject.Find("--- MAP_ROOT ---");
            if (parentObj == null)
            {
                parentObj = new GameObject("--- MAP_ROOT ---");
            }
            tileParent = parentObj.transform;
        }
    }

    private void CreateTile(GridCoords coords)
    {
        if (settings.DefaultTilePrefab == null)
        {
            Debug.LogError("DefaultTilePrefab is not set in Settings!");
            return;
        }

        if (GetEditorTileAt(coords) != null) return;

        FindOrCreateTileParent();
        GameObject newTileObj = (GameObject)PrefabUtility.InstantiatePrefab(settings.DefaultTilePrefab, tileParent);
        Undo.RegisterCreatedObjectUndo(newTileObj, "Create Tile");

        newTileObj.transform.position = GridUtils.GridToWorld(coords);

        var editorTile = newTileObj.GetComponent<EditorTile>();
        if (editorTile != null)
        {
            editorTile.Initialize(coords);
        }
        else
        {
            newTileObj.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";
            Debug.LogWarning($"The default tile prefab is missing an 'EditorTile' component.");
        }

        Debug.Log($"Created tile at {coords}");
    }

    private void ClearMap()
    {
        FindOrCreateTileParent();

        while (tileParent.childCount > 0)
        {
            Undo.DestroyObjectImmediate(tileParent.GetChild(0).gameObject);
        }
    }

    private void LoadMap()
    {
        string path = EditorUtility.OpenFilePanel("Load Map Data", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;

        path = "Assets" + path.Substring(Application.dataPath.Length);
        MapDataSO loadedData = AssetDatabase.LoadAssetAtPath<MapDataSO>(path);

        if (loadedData == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to load MapDataSO from path: " + path, "OK");
            return;
        }

        ClearMap();

        if (settings.DefaultTilePrefab != null)
        {
            foreach (var tileData in loadedData.Tiles)
            {
                if (tileData.FloorID != FloorType.None)
                {
                    CreateTile(tileData.Coords);
                    EditorTile tile = GetEditorTileAt(tileData.Coords);
                    if (tile != null)
                    {
                        Undo.RecordObject(tile, "Load Tile Data");
                        tile.FloorID = tileData.FloorID;
                        tile.Edges = tileData.Edges;
                        EditorUtility.SetDirty(tile);
                    }
                }
            }
        }

        if (settings.DefaultWallPrefab != null)
        {
            foreach (var tileData in loadedData.Tiles)
            {
                for (int i = 0; i < tileData.Edges.Length; i++)
                {
                    SavedEdgeInfo edgeInfo = tileData.Edges[i];
                    if (edgeInfo.Type != EdgeType.Open && edgeInfo.Type != EdgeType.Unknown)
                    {
                        var (neighbor, opposite) = GridUtils.GetOppositeEdge(tileData.Coords, (Direction)i);
                        if (tileData.Coords.CompareTo(neighbor) < 0)
                        {
                            var edge = (tileData.Coords, (Direction)i, GridUtils.GetEdgeWorldPosition(tileData.Coords, (Direction)i), true);

                            selectedEdgeType = edgeInfo.Type;
                            selectedEdgeDataType = edgeInfo.DataType;

                            ModifyEdge(edge);
                        }
                    }
                }
            }
        }

        targetMapData = loadedData;
        Debug.Log($"Map '{loadedData.name}' loaded with {loadedData.Tiles.Count} tiles.");
    }

    private void SaveMap()
    {
        if (targetMapData == null)
        {
            EditorUtility.DisplayDialog("Error", "Target Map Data SO is not assigned.", "OK");
            return;
        }

        var tileDataDict = new Dictionary<GridCoords, TileSaveData>();

        EditorTile[] tilesInScene = FindObjectsOfType<EditorTile>();
        foreach (var editorTile in tilesInScene)
        {
            var saveData = new TileSaveData();
            saveData.InitializeEdges();
            saveData.Coords = editorTile.Coordinate;
            saveData.FloorID = editorTile.FloorID;
            saveData.Edges = editorTile.Edges;
            tileDataDict[editorTile.Coordinate] = saveData;
        }

        EditorPillar[] pillarsInScene = FindObjectsOfType<EditorPillar>();
        foreach (var editorPillar in pillarsInScene)
        {
            if (!tileDataDict.ContainsKey(editorPillar.Coordinate))
            {
                var newSaveData = new TileSaveData();
                newSaveData.InitializeEdges();
                tileDataDict[editorPillar.Coordinate] = newSaveData;
            }

            var saveData = tileDataDict[editorPillar.Coordinate];
            saveData.Coords = editorPillar.Coordinate;
            saveData.PillarID = editorPillar.PillarID;
            tileDataDict[editorPillar.Coordinate] = saveData;
        }

        if (tileDataDict.Count == 0)
        {
            Debug.LogWarning("No tiles or pillars with editor components found. Clearing map data.");
            targetMapData.Tiles.Clear();
            EditorUtility.SetDirty(targetMapData);
            AssetDatabase.SaveAssets();
            return;
        }

        Undo.RecordObject(targetMapData, "Save Map Data");

        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (GridCoords coords in tileDataDict.Keys)
        {
            if (coords.x < minX) minX = coords.x;
            if (coords.x > maxX) maxX = coords.x;
            if (coords.z < minZ) minZ = coords.z;
            if (coords.z > maxZ) maxZ = coords.z;
            if (coords.y < minY) minY = coords.y;
            if (coords.y > maxY) maxY = coords.y;
        }

        targetMapData.GridSize = new Vector2Int(maxX - minX + 1, maxZ - minZ + 1);
        targetMapData.MinLevel = minY;
        targetMapData.MaxLevel = maxY;

        targetMapData.Tiles.Clear();
        foreach (var saveData in tileDataDict.Values)
        {
            targetMapData.Tiles.Add(saveData);
        }

        EditorUtility.SetDirty(targetMapData);
        AssetDatabase.SaveAssets();
        Debug.Log($"Map saved to '{targetMapData.name}' with {targetMapData.Tiles.Count} data points.");
    }
}
