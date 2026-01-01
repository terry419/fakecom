using UnityEngine;
using UnityEditor;

// [리팩토링 2단계] Registry 기반 변환 및 로직 구현 완료
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

    // --- 1. 타일 로직 (Tile Logic) ---

    public void HandleCreateTile(GridCoords coords)
    {
        if (_context.Registry == null)
        {
            Debug.LogError("Context에 Registry가 없습니다!");
            return;
        }

        // 이미 타일이 존재하는지 확인 (중복 생성 방지)
        if (_context.GetTile(coords) != null) return;

        UpdateParentReferences();

        // Registry에서 선택된 타일 정보 가져오기
        var floorEntry = _context.Registry.GetFloor(_context.SelectedFloorType);
        if (floorEntry.Prefab == null)
        {
            Debug.LogError($"해당 바닥 타입의 프리팹이 없습니다: {_context.SelectedFloorType}");
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
            // 타일이 존재할 경우 안전하게 삭제 처리
            SafeDestroy(tile.gameObject);
        }

        // 해당 좌표의 사방 벽면(Edge)들도 함께 삭제
        for (int i = 0; i < 4; i++)
        {
            var wall = _context.GetWall(coords, (Direction)i);
            if (wall != null) SafeDestroy(wall.gameObject);
        }

        _context.InvalidateCache();
    }

    // --- 2. 기둥 로직 (Pillar Logic) ---

    public void HandleCreatePillar(GridCoords coords)
    {
        // 바닥이 없으면 바닥부터 생성
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

            // 시각적 모델 갱신
            RebuildPillarVisual(tile);
            EditorUtility.SetDirty(tile);
        }
    }

    // --- 3. 에지 로직 (Edge Logic - 벽, 창문 등) ---

    public void HandleModifyEdge(GridCoords coords, Direction dir)
    {
        var tile = _context.GetTile(coords);
        // 타일이 있어야만 에지 설치 가능
        if (tile == null) return;

        // Context의 현재 선택값을 기반으로 에지 정보 생성
        SavedEdgeInfo newEdgeInfo = CreateEdgeInfoFromSelection();

        Undo.RecordObject(tile, "Modify Edge");
        tile.Edges[(int)dir] = newEdgeInfo;
        EditorUtility.SetDirty(tile);

        // 시각적 모델 교체 (기존 삭제 -> 신규 생성)
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
                // [수정] Fence는 낮은 엄폐물을 기본으로 생성
                info = SavedEdgeInfo.CreateWall(100f, CoverType.Low);
                info.Type = EdgeType.Fence;
                return info;

            default:
                return SavedEdgeInfo.CreateOpen();
        }
    }

    // --- 4. 로드 및 시각화 도우미 (Load & Visual Helpers) ---

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
                // 데이터 할당
                editorTile.Coordinate = tileData.Coords;
                editorTile.FloorID = tileData.FloorID;
                editorTile.PillarID = tileData.PillarID;

                // 에지 데이터 복사
                if (tileData.Edges != null && tileData.Edges.Length == 4)
                    editorTile.Edges = (SavedEdgeInfo[])tileData.Edges.Clone();
                else
                    editorTile.Edges = new SavedEdgeInfo[4];

                // 기둥 모델 복구
                if (editorTile.PillarID != PillarType.None)
                    RebuildPillarVisual(editorTile);
            }

            // 2. 벽면 시각화 생성
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
        Debug.Log($"[MapEditor] 맵 로드 완료: {data.name}");
    }

    private void RebuildPillarVisual(EditorTile tile)
    {
        // 기존 비주얼 삭제
        var oldPillar = tile.transform.Find("Visual_Pillar");
        if (oldPillar != null) SafeDestroy(oldPillar.gameObject);

        // 기본 바닥(Top_White) 켜기/끄기 처리
        var floorVisual = tile.transform.Find("Top_White");
        if (tile.PillarID == PillarType.None)
        {
            if (floorVisual != null) floorVisual.gameObject.SetActive(true);
            return; // 기둥 없음
        }

        if (floorVisual != null) floorVisual.gameObject.SetActive(false);

        // 새 기둥 프리팹 생성 (Registry 참조)
        var pillarEntry = _context.Registry.GetPillar(tile.PillarID);
        if (pillarEntry.Prefab != null)
        {
            var pObj = (GameObject)PrefabUtility.InstantiatePrefab(pillarEntry.Prefab, tile.transform);
            Undo.RegisterCreatedObjectUndo(pObj, "Pillar Visual");
            pObj.name = "Visual_Pillar";

            // [Fix] 기둥도 바닥 높이에 맞춰 정렬 (기존에는 이 부분이 없어서 묻힘)
            // 타일의 Y 위치를 기준으로 정렬합니다.
            AlignToGround(pObj, tile.transform.position.y);
        }
    }

    private void CreateWallVisual(GridCoords coords, Direction dir, SavedEdgeInfo info)
    {
        // 타입에 맞는 프리팹 가져오기
        var edgeEntry = _context.Registry.GetEdge(info.Type);
        if (edgeEntry.Prefab == null) return;

        var wObj = (GameObject)PrefabUtility.InstantiatePrefab(edgeEntry.Prefab, _edgeParent);
        Undo.RegisterCreatedObjectUndo(wObj, "Create Wall");

        // 위치 및 회전 설정
        var pos = GridUtils.GetEdgeWorldPosition(coords, dir);

        // 1. 기본 위치 설정
        wObj.transform.position = pos;

        // 2. 방향에 따른 회전 (북/남은 정방향, 동/서는 90도 회전)
        wObj.transform.rotation = (dir == Direction.North || dir == Direction.South)
            ? Quaternion.identity
            : Quaternion.Euler(0, 90, 0);

        // 3. [수정] 지면 높이 자동 정렬 (모델의 하단 피벗을 바닥에 맞춤)
        AlignToGround(wObj, pos.y);

        var ew = wObj.GetComponent<EditorWall>();
        if (ew != null) ew.Initialize(coords, dir, info);
    }

    /// <summary>
    /// [수정된 로직] 렌더러의 Bounds를 사용하여 모델의 최하단(min.y)이 목표 높이(targetY)에 오도록 보정합니다.
    /// 모델마다 피벗 위치가 다르더라도 항상 바닥 위에 정확히 배치되게 합니다.
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

        // 현재 최하단 Y값과 목표 Y값의 차이만큼 이동
        float currentMinY = combinedBounds.min.y;
        float diff = targetY - currentMinY;

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

    // [SafeDestroy] Undo를 지원하고 선택 상태를 관리하는 삭제 함수
    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;

        // 현재 선택된 객체가 삭제될 대상이거나 그 자식일 경우 선택 해제
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