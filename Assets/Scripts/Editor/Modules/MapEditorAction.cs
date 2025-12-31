using UnityEngine;
using UnityEditor;

// [Refactoring Phase 2] Registry 기반 전환 및 높이 자동 보정 적용 완료
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
        if (_context.Registry == null)
        {
            Debug.LogError("Registry is missing in Context!");
            return;
        }

        // 이미 타일이 있으면 생성하지 않음 (중복 방지)
        if (_context.GetTile(coords) != null) return;

        UpdateParentReferences();

        // Registry에서 현재 선택된 바닥 타입의 프리팹 가져오기
        var floorEntry = _context.Registry.GetFloor(_context.SelectedFloorType);
        if (floorEntry.Prefab == null)
        {
            Debug.LogError($"Prefab missing for floor type: {_context.SelectedFloorType}");
            return;
        }

        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(floorEntry.Prefab, _tileParent);
        Undo.RegisterCreatedObjectUndo(newObj, "Create Tile");

        newObj.transform.position = GridUtils.GridToWorld(coords);
        newObj.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";

        var editorTile = newObj.GetComponent<EditorTile>();
        if (editorTile != null)
        {
            editorTile.Initialize(coords);
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

        // Context에서 선택된 타입으로 데이터 생성
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
        var type = _context.SelectedEdgeType;
        SavedEdgeInfo info;

        switch (type)
        {
            case EdgeType.Wall:
                return SavedEdgeInfo.CreateWall();

            case EdgeType.Window:
                return SavedEdgeInfo.CreateWindow();

            case EdgeType.Door:
                return SavedEdgeInfo.CreateDoor();

            case EdgeType.Fence:
                // [Fix] Fence 선택 시 Fence 타입이 들어가도록 명시적 할당
                info = SavedEdgeInfo.CreateWall(100f, CoverType.Low);
                info.Type = EdgeType.Fence;
                return info;

            default:
                return SavedEdgeInfo.CreateOpen();
        }
    }

    // --- 4. Load & Visual Helpers ---

    public void LoadMapFromData(MapDataSO data)
    {
        if (data == null || _context.Registry == null) return;

        UpdateParentReferences();
        ClearMap();

        Undo.SetCurrentGroupName("Load Map");
        int group = Undo.GetCurrentGroup();

        foreach (var tileData in data.Tiles)
        {
            // 1. 타일 생성
            var floorEntry = _context.Registry.GetFloor(tileData.FloorID);
            if (floorEntry.Prefab == null) continue;

            var obj = (GameObject)PrefabUtility.InstantiatePrefab(floorEntry.Prefab, _tileParent);
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

        // 새 기둥 생성 (Registry 사용)
        var pillarEntry = _context.Registry.GetPillar(tile.PillarID);
        if (pillarEntry.Prefab != null)
        {
            var pObj = (GameObject)PrefabUtility.InstantiatePrefab(pillarEntry.Prefab, tile.transform);
            Undo.RegisterCreatedObjectUndo(pObj, "Pillar Visual");
            pObj.name = "Visual_Pillar";
        }
    }

    private void CreateWallVisual(GridCoords coords, Direction dir, SavedEdgeInfo info)
    {
        // 타입에 맞는 프리팹 선정 (Registry 사용)
        var edgeEntry = _context.Registry.GetEdge(info.Type);
        if (edgeEntry.Prefab == null) return;

        var wObj = (GameObject)PrefabUtility.InstantiatePrefab(edgeEntry.Prefab, _edgeParent);
        Undo.RegisterCreatedObjectUndo(wObj, "Create Wall");

        // 위치/회전 설정
        var pos = GridUtils.GetEdgeWorldPosition(coords, dir);

        // 1. 일단 정위치에 배치
        wObj.transform.position = pos;

        // 2. 회전 적용
        wObj.transform.rotation = (dir == Direction.North || dir == Direction.South)
            ? Quaternion.identity
            : Quaternion.Euler(0, 90, 0);

        // 3. [Fix] 자동 높이 보정 (BoundsMin을 바닥에 맞춤)
        AlignToGround(wObj, pos.y);

        var ew = wObj.GetComponent<EditorWall>();
        if (ew != null) ew.Initialize(coords, dir, info);
    }

    /// <summary>
    /// [핵심 기능] 모델의 렌더러 Bounds를 계산하여, 최하단(min.y)을 목표 높이(targetY)에 딱 맞춥니다.
    /// 프리팹 피벗이 중앙이든, 위든, 아래든 상관없이 바닥에 붙게 만듭니다.
    /// </summary>
    private void AlignToGround(GameObject obj, float targetY)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // 전체 Bounds 계산 (World Space AABB)
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // 현재 모델의 가장 낮은 지점
        float currentMinY = combinedBounds.min.y;

        // 목표 높이와의 차이 계산
        float diff = targetY - currentMinY;

        // 위치 보정 적용
        obj.transform.position += new Vector3(0, diff, 0);
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

    // [SafeDestroy] 인스펙터 참조 오류 방지용
    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;

        // 현재 선택된 오브젝트가 삭제하려는 오브젝트 자신이거나, 그 자식이라면 선택을 해제합니다.
        if (Selection.activeGameObject != null)
        {
            if (Selection.activeGameObject == go || Selection.activeGameObject.transform.IsChildOf(go.transform))
            {
                Selection.activeGameObject = null;
            }
        }

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