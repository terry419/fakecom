// 파일: Assets/Scripts/Editor/Modules/MapEditorAction.cs
using UnityEngine;
using UnityEditor;

public class MapEditorAction
{
    private readonly MapEditorContext _context;
    private Transform _tileParent;
    private Transform _edgeParent;

    public MapEditorAction(MapEditorContext context)
    {
        _context = context;
        UpdateParentReferences();
    }

    // ... (HandleCreateTile 등 기존 코드는 유지하되, 아래 RebuildPillarVisual 수정이 핵심입니다) ...

    public void HandleCreateTile(GridCoords coords)
    {
        // (기존과 동일)
        if (_context.Settings.DefaultTilePrefab == null) return;
        if (_context.GetTile(coords) != null) return;

        UpdateParentReferences();
        GameObject newTileObj = (GameObject)PrefabUtility.InstantiatePrefab(_context.Settings.DefaultTilePrefab, _tileParent);
        Undo.RegisterCreatedObjectUndo(newTileObj, "Create Tile");

        newTileObj.transform.position = GridUtils.GridToWorld(coords);
        newTileObj.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";

        var editorTile = newTileObj.GetComponent<EditorTile>();
        if (editorTile != null) editorTile.Initialize(coords);

        _context.InvalidateCache();
    }

    public void HandleCreatePillar(GridCoords coords)
    {
        EditorTile tile = _context.GetTile(coords);

        if (tile == null)
        {
            HandleCreateTile(coords);
            tile = _context.GetTile(coords);
        }

        if (tile != null)
        {
            Undo.RecordObject(tile, "Set Pillar");
            tile.PillarID = _context.SelectedPillarType;

            // [중요] 비주얼 갱신 (바닥 숨김 처리 포함)
            RebuildPillarVisual(tile);

            EditorUtility.SetDirty(tile);
        }
    }

    // ... (HandleModifyEdge, HandleEraseTile 등 기존 유지) ...
    public void HandleModifyEdge(GridCoords coords, Direction dir)
    {
        var newEdgeData = CreateEdgeDataFromContext();
        var tile = _context.GetTile(coords);
        if (tile != null)
        {
            Undo.RecordObject(tile, "Modify Edge");
            tile.Edges[(int)dir] = newEdgeData;
            EditorUtility.SetDirty(tile);
        }
        ReplaceEdgeVisuals(coords, dir, newEdgeData);
        _context.InvalidateCache();
    }

    public void HandleEraseTile(GridCoords coords)
    {
        var tile = _context.GetTile(coords);
        if (tile != null) Undo.DestroyObjectImmediate(tile.gameObject);

        for (int i = 0; i < 4; i++)
        {
            var wall = _context.GetWall(coords, (Direction)i);
            if (wall != null) Undo.DestroyObjectImmediate(wall.gameObject);
        }
        _context.InvalidateCache();
    }


    // [Fix Issue 2] 로드 시 기존 맵 클리어 후 생성
    public void LoadMapFromData(MapDataSO data)
    {
        if (data == null) return;

        UpdateParentReferences();
        ClearMap(); // 깔끔하게 지우고 시작

        Undo.SetCurrentGroupName("Load Map");
        int group = Undo.GetCurrentGroup();

        foreach (var tileData in data.Tiles)
        {
            if (_context.Settings.DefaultTilePrefab == null) continue;

            var tileObj = (GameObject)PrefabUtility.InstantiatePrefab(_context.Settings.DefaultTilePrefab, _tileParent);
            Undo.RegisterCreatedObjectUndo(tileObj, "Load Tile");

            tileObj.transform.position = GridUtils.GridToWorld(tileData.Coords);
            tileObj.name = $"Tile_{tileData.Coords.x}_{tileData.Coords.z}_{tileData.Coords.y}";

            var editorTile = tileObj.GetComponent<EditorTile>();
            if (editorTile != null)
            {
                editorTile.Coordinate = tileData.Coords;
                editorTile.FloorID = tileData.FloorID;
                editorTile.PillarID = tileData.PillarID;

                if (tileData.Edges != null && tileData.Edges.Length == 4)
                    editorTile.Edges = (SavedEdgeInfo[])tileData.Edges.Clone();
                else
                    editorTile.Edges = new SavedEdgeInfo[4];

                // 로드할 때도 Pillar 상태에 따라 바닥 On/Off 처리
                if (editorTile.PillarID != PillarType.None)
                {
                    RebuildPillarVisual(editorTile);
                }
            }

            for (int i = 0; i < 4; i++)
            {
                if (editorTile == null) break;
                SavedEdgeInfo edgeInfo = editorTile.Edges[i];
                if (edgeInfo.Type != EdgeType.Open && edgeInfo.Type != EdgeType.Unknown)
                    CreateWallVisual(tileData.Coords, (Direction)i, edgeInfo);
            }
        }

        Undo.CollapseUndoOperations(group);
        _context.InvalidateCache();
        Debug.Log($"[MapEditor] Map Loaded from {data.name}");
    }

    // --- Helpers ---

    // [Fix Issue 1] 기둥 생성 시 바닥(Top_White) 숨기기
    private void RebuildPillarVisual(EditorTile tile)
    {
        // 1. 기존 기둥 비주얼 삭제
        Transform existingPillar = tile.transform.Find("Visual_Pillar");
        if (existingPillar != null) Undo.DestroyObjectImmediate(existingPillar.gameObject);

        // 2. 바닥 비주얼 찾기 (이름은 프리팹 구조에 따라 다를 수 있음, 여기서는 'Top_White' 가정)
        // 만약 이름이 확실치 않다면 transform.GetChild(0) 등으로 접근하거나 Tag를 사용해야 합니다.
        Transform floorVisual = tile.transform.Find("Top_White");

        // 기둥이 없으면 -> 바닥 보여주기
        if (tile.PillarID == PillarType.None)
        {
            if (floorVisual != null) floorVisual.gameObject.SetActive(true);
            return;
        }

        // 기둥이 있으면 -> 바닥 숨기기 (Visual Replacement)
        if (floorVisual != null) floorVisual.gameObject.SetActive(false);

        // 3. 새 기둥 생성
        GameObject pillarPrefab = GetPrefabForPillarType(tile.PillarID);
        if (pillarPrefab != null)
        {
            GameObject pillarObj = (GameObject)PrefabUtility.InstantiatePrefab(pillarPrefab, tile.transform);
            Undo.RegisterCreatedObjectUndo(pillarObj, "Create Pillar Visual");
            pillarObj.name = "Visual_Pillar";

            // 스케일 보정 (이전 답변 내용 유지)
            Vector3 parentScale = tile.transform.localScale;
            Vector3 originalScale = pillarPrefab.transform.localScale;
            Vector3 originalPos = pillarPrefab.transform.localPosition;

            float scaleX = (Mathf.Abs(parentScale.x) > 0.001f) ? (1f / parentScale.x) : 1f;
            float scaleY = (Mathf.Abs(parentScale.y) > 0.001f) ? (1f / parentScale.y) : 1f;
            float scaleZ = (Mathf.Abs(parentScale.z) > 0.001f) ? (1f / parentScale.z) : 1f;

            pillarObj.transform.localScale = new Vector3(originalScale.x * scaleX, originalScale.y * scaleY, originalScale.z * scaleZ);
            pillarObj.transform.localPosition = new Vector3(originalPos.x * scaleX, originalPos.y * scaleY, originalPos.z * scaleZ);
        }
    }

    // ... (ClearMap, UpdateParentReferences, CreateWallVisual 등은 이전 답변과 동일하게 유지) ...
    private void UpdateParentReferences()
    {
        if (_tileParent == null)
        {
            var go = GameObject.Find("MapEditor_Tiles");
            if (go == null) go = new GameObject("MapEditor_Tiles");
            _tileParent = go.transform;
        }
        if (_edgeParent == null)
        {
            var go = GameObject.Find("MapEditor_Edges");
            if (go == null) go = new GameObject("MapEditor_Edges");
            _edgeParent = go.transform;
        }
    }

    private void ClearMap()
    {
        if (_tileParent != null)
        {
            for (int i = _tileParent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(_tileParent.GetChild(i).gameObject);
        }
        if (_edgeParent != null)
        {
            for (int i = _edgeParent.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(_edgeParent.GetChild(i).gameObject);
        }
        _context.InvalidateCache();
    }

    // ... (이하 CreateWallVisual, Helpers 생략 - 이전 답변 코드를 사용해주세요) ...
    private void CreateWallVisual(GridCoords coords, Direction dir, SavedEdgeInfo edgeInfo)
    {
        // 이전 답변의 CreateWallVisual 복사
        GameObject wallPrefab = GetPrefabForEdgeType(edgeInfo.Type);
        if (wallPrefab == null) return;

        GameObject wallObj = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab, _edgeParent);
        Undo.RegisterCreatedObjectUndo(wallObj, "Create Wall");

        Vector3 edgePos = GridUtils.GetEdgeWorldPosition(coords, dir);
        wallObj.transform.position = edgePos + new Vector3(0, GridUtils.LEVEL_HEIGHT * 0.5f, 0);
        wallObj.transform.rotation = (dir == Direction.North || dir == Direction.South) ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

        var editorWall = wallObj.GetComponent<EditorWall>();
        if (editorWall != null) editorWall.Initialize(coords, dir, edgeInfo);
    }

    private SavedEdgeInfo CreateEdgeDataFromContext()
    {
        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge)
        {
            switch (_context.SelectedEdgeType)
            {
                case EdgeType.Wall: return SavedEdgeInfo.CreateWall(_context.SelectedEdgeDataType);
                case EdgeType.Window: return SavedEdgeInfo.CreateWindow(_context.SelectedEdgeDataType);
                case EdgeType.Door: return SavedEdgeInfo.CreateDoor(_context.SelectedEdgeDataType);
            }
        }
        return SavedEdgeInfo.CreateOpen();
    }

    private void ReplaceEdgeVisuals(GridCoords coords, Direction dir, SavedEdgeInfo edgeInfo)
    {
        var existingWall = _context.GetWall(coords, dir);
        if (existingWall != null) Undo.DestroyObjectImmediate(existingWall.gameObject);
        if (edgeInfo.Type == EdgeType.Open) return;
        CreateWallVisual(coords, dir, edgeInfo);
    }

    private GameObject GetPrefabForPillarType(PillarType type) => _context.Settings.DefaultPillarPrefab;
    private GameObject GetPrefabForEdgeType(EdgeType type)
    {
        switch (type)
        {
            case EdgeType.Wall: return _context.Settings.DefaultWallPrefab;
            case EdgeType.Window: return _context.Settings.DefaultWindowPrefab;
            case EdgeType.Door: return _context.Settings.DefaultDoorPrefab;
            default: return null;
        }
    }
}