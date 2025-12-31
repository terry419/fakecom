using UnityEngine;
using UnityEditor;

// [Refactoring] 실제 씬 오브젝트의 생성(Create), 수정(Modify), 삭제(Destroy)만 담당
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

    // --- 1. Tile Logic ---

    public void HandleCreateTile(GridCoords coords)
    {
        if (_context.Settings == null || _context.Settings.DefaultTilePrefab == null) return;

        // 이미 타일이 있으면 생성하지 않음 (중복 방지)
        if (_context.GetTile(coords) != null) return;

        UpdateParentReferences();

        // 프리팹 생성 및 Undo 등록
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(_context.Settings.DefaultTilePrefab, _tileParent);
        Undo.RegisterCreatedObjectUndo(newObj, "Create Tile");

        // 위치 및 이름 설정
        newObj.transform.position = GridUtils.GridToWorld(coords);
        newObj.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";

        // 컴포넌트 초기화
        var editorTile = newObj.GetComponent<EditorTile>();
        if (editorTile != null)
        {
            editorTile.Initialize(coords);
            // 생성 시 바닥 타입 적용 (Context의 선택값)
            editorTile.FloorID = _context.SelectedFloorType;
        }

        _context.InvalidateCache();
    }

    public void HandleEraseTile(GridCoords coords)
    {
        var tile = _context.GetTile(coords);
        if (tile != null)
        {
            // 타일 삭제 시 위에 있는 기둥, 연결된 벽도 같이 처리해야 깔끔함
            // 현재는 타일 게임오브젝트만 삭제
            SafeDestroy(tile.gameObject);
        }

        // 해당 좌표의 벽들도 삭제
        for (int i = 0; i < 4; i++)
        {
            var wall = _context.GetWall(coords, (Direction)i);
            if (wall != null) SafeDestroy(wall.gameObject);
        }

        _context.InvalidateCache();
    }

    // --- 2. Pillar Logic ---

    public void HandleCreatePillar(GridCoords coords)
    {
        // 타일이 없으면 타일 먼저 생성
        var tile = _context.GetTile(coords);
        if (tile == null)
        {
            HandleCreateTile(coords);
            tile = _context.GetTile(coords);
        }

        if (tile != null)
        {
            Undo.RecordObject(tile, "Set Pillar");
            tile.PillarID = _context.SelectedPillarType;

            // 시각적 갱신
            RebuildPillarVisual(tile);
            EditorUtility.SetDirty(tile);
        }
    }

    // --- 3. Edge Logic ---

    public void HandleModifyEdge(GridCoords coords, Direction dir)
    {
        var tile = _context.GetTile(coords);
        // 타일이 있어야 벽을 세울 수 있음
        if (tile == null) return;

        // Context에서 선택된 타입으로 데이터 생성 (EdgeDataType 땜질 제거됨)
        SavedEdgeInfo newEdgeInfo = CreateEdgeInfoFromSelection();

        Undo.RecordObject(tile, "Modify Edge");
        tile.Edges[(int)dir] = newEdgeInfo;
        EditorUtility.SetDirty(tile);

        // 시각적 갱신 (기존 벽 삭제 -> 새 벽 생성)
        ReplaceEdgeVisuals(coords, dir, newEdgeInfo);

        _context.InvalidateCache();
    }

    private SavedEdgeInfo CreateEdgeInfoFromSelection()
    {
        // EdgeDataType 파라미터가 삭제되었으므로, Factory 메서드 호출 시 인자 제거
        // EdgeFactory가 내부적으로 기본값을 처리하도록 변경되었어야 함 (이전 단계 반영)
        // 여기서는 명시적으로 'None' 처리하거나 팩토리 패턴을 따름

        var type = _context.SelectedEdgeType;
        // Registry 도입으로 재질(EdgeDataType)은 프리팹에 종속되므로 
        // 여기서는 타입(Wall/Window/Door)만 결정하면 됨.

        switch (type)
        {
            case EdgeType.Wall: return SavedEdgeInfo.CreateWall(EdgeDataType.None);
            case EdgeType.Window: return SavedEdgeInfo.CreateWindow(EdgeDataType.None);
            case EdgeType.Door: return SavedEdgeInfo.CreateDoor(EdgeDataType.None);
            default: return SavedEdgeInfo.CreateOpen();
        }
    }

    // --- 4. Load & Visual Helpers ---

    public void LoadMapFromData(MapDataSO data)
    {
        if (data == null || _context.Settings.DefaultTilePrefab == null) return;

        UpdateParentReferences();
        ClearMap();

        Undo.SetCurrentGroupName("Load Map");
        int group = Undo.GetCurrentGroup();

        foreach (var tileData in data.Tiles)
        {
            // 1. 타일 생성
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(_context.Settings.DefaultTilePrefab, _tileParent);
            Undo.RegisterCreatedObjectUndo(obj, "Load Tile");
            obj.transform.position = GridUtils.GridToWorld(tileData.Coords);
            obj.name = $"Tile_{tileData.Coords.x}_{tileData.Coords.z}_{tileData.Coords.y}";

            var editorTile = obj.GetComponent<EditorTile>();
            if (editorTile != null)
            {
                // 데이터 복원
                editorTile.Coordinate = tileData.Coords;
                editorTile.FloorID = tileData.FloorID;
                editorTile.PillarID = tileData.PillarID;

                // Edge 데이터 깊은 복사
                if (tileData.Edges != null && tileData.Edges.Length == 4)
                    editorTile.Edges = (SavedEdgeInfo[])tileData.Edges.Clone();
                else
                    editorTile.Edges = new SavedEdgeInfo[4];

                // 기둥 비주얼 복원
                if (editorTile.PillarID != PillarType.None)
                    RebuildPillarVisual(editorTile);
            }

            // 2. 벽 생성 (Visual)
            for (int i = 0; i < 4; i++)
            {
                if (editorTile == null) break;
                var edgeInfo = editorTile.Edges[i];
                if (edgeInfo.Type != EdgeType.Open)
                {
                    CreateWallVisual(tileData.Coords, (Direction)i, edgeInfo);
                }
            }
        }

        Undo.CollapseUndoOperations(group);
        _context.InvalidateCache();
        Debug.Log($"[MapEditor] Map Loaded: {data.name}");
    }

    private void RebuildPillarVisual(EditorTile tile)
    {
        // 기존 기둥 제거
        var oldPillar = tile.transform.Find("Visual_Pillar");
        if (oldPillar != null) SafeDestroy(oldPillar.gameObject);

        // 기본 바닥(Top_White) 숨김/표시 처리
        var floorVisual = tile.transform.Find("Top_White");
        if (tile.PillarID == PillarType.None)
        {
            if (floorVisual != null) floorVisual.gameObject.SetActive(true);
            return; // 기둥 없으면 종료
        }

        if (floorVisual != null) floorVisual.gameObject.SetActive(false);

        // 새 기둥 생성 (Registry가 아니라 EditorSettings의 Default Prefab 사용)
        // 에디터에서는 시각적 확인만 하면 되므로 Default 프리팹 사용이 적절함
        var prefab = _context.Settings.DefaultPillarPrefab;
        if (prefab != null)
        {
            var pObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, tile.transform);
            Undo.RegisterCreatedObjectUndo(pObj, "Pillar Visual");
            pObj.name = "Visual_Pillar";
        }
    }

    private void CreateWallVisual(GridCoords coords, Direction dir, SavedEdgeInfo info)
    {
        // 타입에 맞는 프리팹 선정
        GameObject prefab = null;
        switch (info.Type)
        {
            case EdgeType.Wall: prefab = _context.Settings.DefaultWallPrefab; break;
            case EdgeType.Window: prefab = _context.Settings.DefaultWindowPrefab; break;
            case EdgeType.Door: prefab = _context.Settings.DefaultDoorPrefab; break;
        }

        if (prefab == null) return;

        var wObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, _edgeParent);
        Undo.RegisterCreatedObjectUndo(wObj, "Create Wall");

        // 위치/회전 설정
        var pos = GridUtils.GetEdgeWorldPosition(coords, dir);
        // 벽 높이 보정 (중심점이 바닥이면 y 올려야 함)
        wObj.transform.position = pos + new Vector3(0, GridUtils.LEVEL_HEIGHT * 0.5f, 0);
        wObj.transform.rotation = (dir == Direction.North || dir == Direction.South)
            ? Quaternion.identity
            : Quaternion.Euler(0, 90, 0);

        var ew = wObj.GetComponent<EditorWall>();
        if (ew != null) ew.Initialize(coords, dir, info);
    }

    private void ReplaceEdgeVisuals(GridCoords coords, Direction dir, SavedEdgeInfo newInfo)
    {
        var existingWall = _context.GetWall(coords, dir);
        if (existingWall != null) SafeDestroy(existingWall.gameObject);

        if (newInfo.Type != EdgeType.Open)
        {
            CreateWallVisual(coords, dir, newInfo);
        }
    }

    // [SafeDestroy] 인스펙터 참조 오류 방지용 래퍼
    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        if (Selection.activeGameObject == go) Selection.activeGameObject = null;
        Undo.DestroyObjectImmediate(go);
    }

    private void UpdateParentReferences()
    {
        if (_tileParent == null) _tileParent = EnsureParent("MapEditor_Tiles");
        if (_edgeParent == null) _edgeParent = EnsureParent("MapEditor_Edges");
    }

    private Transform EnsureParent(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go.transform;
    }

    private void ClearMap()
    {
        if (_tileParent != null)
        {
            for (int i = _tileParent.childCount - 1; i >= 0; i--)
                SafeDestroy(_tileParent.GetChild(i).gameObject);
        }
        if (_edgeParent != null)
        {
            for (int i = _edgeParent.childCount - 1; i >= 0; i--)
                SafeDestroy(_edgeParent.GetChild(i).gameObject);
        }
        _context.InvalidateCache();
    }
}