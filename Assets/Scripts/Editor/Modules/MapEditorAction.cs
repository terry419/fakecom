using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class MapEditorAction
{
    private readonly MapEditorContext _context;

    // 계층 구조 관리용 부모
    private Transform _tileParent;
    private Transform _edgeParent;
    private Transform _markerParent; // [New] 마커 부모 추가

    public MapEditorAction(MapEditorContext context)
    {
        _context = context;
        UpdateParentReferences();
    }

    // ==================================================================================
    // 1. 타일 로직 (Tile Logic)
    // ==================================================================================
    public void HandleCreateTile(GridCoords coords)
    {
        if (_context.Registry == null) { Debug.LogError("Context에 Registry가 없습니다!"); return; }
        // 이미 타일이 있으면 패스
        if (_context.GetTile(coords) != null) return;

        UpdateParentReferences();

        var floorEntry = _context.Registry.GetFloor(_context.SelectedFloorType);
        if (floorEntry.Prefab == null) return;

        // 타일 생성
        GameObject newObj = (GameObject)PrefabUtility.InstantiatePrefab(floorEntry.Prefab, _tileParent);
        Undo.RegisterCreatedObjectUndo(newObj, "Create Tile");

        newObj.transform.position = GridUtils.GridToWorld(coords);
        newObj.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";

        var editorTile = newObj.GetComponent<EditorTile>();
        if (editorTile != null)
        {
            editorTile.Initialize(coords);
            editorTile.FloorID = _context.SelectedFloorType;
            // [Important] PortalData는 이제 사용하지 않으므로 null 처리
            editorTile.PortalData = null;
        }

        _context.InvalidateCache();
    }

    public void HandleEraseTile(GridCoords coords)
    {
        // 1. 타일 삭제
        var tile = _context.GetTile(coords);
        if (tile != null) SafeDestroy(tile.gameObject);

        // 2. 벽 삭제
        for (int i = 0; i < 4; i++)
        {
            var wall = _context.GetWall(coords, (Direction)i);
            if (wall != null) SafeDestroy(wall.gameObject);
        }

        // 3. [New] 해당 위치의 마커도 함께 삭제 (바닥이 없으면 마커도 의미 없음)
        HandleRemoveMarker(coords);

        _context.InvalidateCache();
    }

    // ==================================================================================
    // 2. 기둥 로직 (Pillar Logic)
    // ==================================================================================
    public void HandleCreatePillar(GridCoords coords)
    {
        var tile = _context.GetTile(coords);
        // 타일 없으면 자동 생성
        if (tile == null)
        {
            HandleCreateTile(coords);
            tile = _context.GetTile(coords);
        }

        if (tile != null)
        {
            Undo.RecordObject(tile, "Set Pillar");
            tile.PillarID = _context.SelectedPillarType;
            RebuildPillarVisual(tile);
            EditorUtility.SetDirty(tile);
        }
    }

    // ==================================================================================
    // 3. [New] 마커 로직 (Marker Logic - Portal & Spawn)
    // ==================================================================================

    // 포탈 생성
    public void HandleCreatePortal(GridCoords coords, PortalType pType, string id, Direction facing)
    {
        if (_context.Registry == null) return;

        // 바닥 타일 보장
        EnsureBaseTile(coords);
        // 기존 마커 제거 (중복 방지)
        HandleRemoveMarker(coords);
        UpdateParentReferences();

        // 오브젝트 생성
        GameObject markerObj = new GameObject($"Portal_{pType}_{id}");
        SetupMarkerObject(markerObj, coords);

        // 마커 컴포넌트 설정
        var marker = markerObj.AddComponent<EditorMarker>();
        marker.MarkerCategory = MarkerType.Portal;
        marker.PType = pType;
        marker.ID = id;
        marker.Facing = facing;

        // 비주얼 생성 (Registry에서 Type 기반 조회)
        // [Key Change] 수동 생성 코드가 사라지고 프리팹 인스턴스화로 대체됨
        var entry = _context.Registry.GetPortalPrefab(pType);
        if (entry.Prefab != null)
        {
            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(entry.Prefab, markerObj.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning($"[MapEditorAction] No prefab found for PortalType: {pType}");
        }

        // Gizmo 색상 설정
        marker.GizmoColor = (pType == PortalType.In) ? _context.Registry.PortalInColor : _context.Registry.PortalOutColor;

        Undo.RegisterCreatedObjectUndo(markerObj, "Create Portal");
    }

    // 스폰 생성
    public void HandleCreateSpawn(GridCoords coords, MarkerType sType, string roleTag)
    {
        EnsureBaseTile(coords);
        HandleRemoveMarker(coords);
        UpdateParentReferences();

        GameObject markerObj = new GameObject($"Spawn_{sType}_{roleTag}");
        SetupMarkerObject(markerObj, coords);

        var marker = markerObj.AddComponent<EditorMarker>();
        marker.MarkerCategory = sType;
        marker.ID = roleTag;

        // [New] 스폰 비주얼(프리팹) 생성 로직 추가
        if (_context.Registry != null)
        {
            // TileRegistrySO에 추가된 GetSpawnPrefab 사용
            GameObject prefab = _context.Registry.GetSpawnPrefab(sType);
            if (prefab != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, markerObj.transform);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // 프리팹 없으면 디버깅용 캡슐
                Debug.Log($"No prefab for {sType}, using default visual.");
                var temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                temp.transform.SetParent(markerObj.transform);
                temp.transform.localPosition = Vector3.up * 1f;
                temp.transform.localScale = Vector3.one * 0.5f;
            }

            // Gizmo 색상도 설정
            marker.GizmoColor = (sType == MarkerType.PlayerSpawn) ?
                _context.Registry.PlayerSpawnColor : _context.Registry.EnemySpawnColor;
        }

        Undo.RegisterCreatedObjectUndo(markerObj, "Create Spawn");
    }

    // 마커 삭제 (공통)
    public void HandleRemoveMarker(GridCoords coords)
    {
        // 씬 전체 마커 검색 (성능 최적화가 필요하다면 추후 캐싱)
        var allMarkers = Object.FindObjectsOfType<EditorMarker>();
        foreach (var m in allMarkers)
        {
            if (GridUtils.WorldToGrid(m.transform.position).Equals(coords))
            {
                Undo.DestroyObjectImmediate(m.gameObject);
            }
        }
    }

    // 마커 위치 설정 헬퍼
    private void SetupMarkerObject(GameObject obj, GridCoords coords)
    {
        obj.transform.SetParent(_markerParent);
        // 바닥과 겹치지 않게 살짝 위로
        obj.transform.position = GridUtils.GridToWorld(coords) + Vector3.up * 0.5f;
    }

    // ==================================================================================
    // 4. 에지 로직 (Edge Logic)
    // ==================================================================================
    public void HandleModifyEdge(GridCoords coords, Direction dir)
    {
        var tile = _context.GetTile(coords);
        if (tile == null) return;

        SavedEdgeInfo newEdgeInfo = CreateEdgeInfoFromSelection();

        Undo.RecordObject(tile, "Modify Edge");
        tile.Edges[(int)dir] = newEdgeInfo;
        EditorUtility.SetDirty(tile);

        ReplaceEdgeVisuals(coords, dir, newEdgeInfo);
        _context.InvalidateCache();
    }

    private SavedEdgeInfo CreateEdgeInfoFromSelection()
    {
        var type = _context.SelectedEdgeType;
        if (type == EdgeType.Wall) return SavedEdgeInfo.CreateWall();
        if (type == EdgeType.Window) return SavedEdgeInfo.CreateWindow();
        if (type == EdgeType.Door) return SavedEdgeInfo.CreateDoor();
        if (type == EdgeType.Fence)
        {
            var info = SavedEdgeInfo.CreateWall(100f, CoverType.Low);
            info.Type = EdgeType.Fence;
            return info;
        }
        return SavedEdgeInfo.CreateOpen();
    }

    // ==================================================================================
    // 5. 로드 및 시각화 (Load & Visual)
    // ==================================================================================
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
                editorTile.Initialize(tileData.Coords);
                editorTile.FloorID = tileData.FloorID;
                editorTile.PillarID = tileData.PillarID;

                // [Changed] 구식 PortalData 로드 로직 제거
                editorTile.PortalData = null;

                if (tileData.Edges != null && tileData.Edges.Length == 4)
                    editorTile.Edges = (SavedEdgeInfo[])tileData.Edges.Clone();
                else
                {
                    editorTile.Edges = new SavedEdgeInfo[4];
                    for (int i = 0; i < 4; i++) editorTile.Edges[i] = SavedEdgeInfo.CreateOpen();
                }

                if (editorTile.PillarID != PillarType.None) RebuildPillarVisual(editorTile);
            }

            // 2. 벽면 생성
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

    // --- Visual Helpers (기존 기능 유지) ---

    private void RebuildPillarVisual(EditorTile tile)
    {
        var oldPillar = tile.transform.Find("Visual_Pillar");
        if (oldPillar != null) SafeDestroy(oldPillar.gameObject);

        var floorVisual = tile.transform.Find("Top_White");
        if (tile.PillarID == PillarType.None)
        {
            if (floorVisual != null) floorVisual.gameObject.SetActive(true);
            return;
        }

        if (floorVisual != null) floorVisual.gameObject.SetActive(false);

        var pillarEntry = _context.Registry.GetPillar(tile.PillarID);
        if (pillarEntry.Prefab != null)
        {
            var pObj = (GameObject)PrefabUtility.InstantiatePrefab(pillarEntry.Prefab, tile.transform);
            Undo.RegisterCreatedObjectUndo(pObj, "Pillar Visual");
            pObj.name = "Visual_Pillar";
            AlignToGround(pObj, tile.transform.position.y);
        }
    }

    private void CreateWallVisual(GridCoords coords, Direction dir, SavedEdgeInfo info)
    {
        var edgeEntry = _context.Registry.GetEdge(info.Type);
        if (edgeEntry.Prefab == null) return;

        var wObj = (GameObject)PrefabUtility.InstantiatePrefab(edgeEntry.Prefab, _edgeParent);
        Undo.RegisterCreatedObjectUndo(wObj, "Create Wall");

        var pos = GridUtils.GetEdgeWorldPosition(coords, dir);
        wObj.transform.position = pos;
        wObj.transform.rotation = (dir == Direction.North || dir == Direction.South)
            ? Quaternion.identity
            : Quaternion.Euler(0, 90, 0);

        AlignToGround(wObj, pos.y);

        var ew = wObj.GetComponent<EditorWall>();
        if (ew != null) ew.Initialize(coords, dir, info);
    }

    private void AlignToGround(GameObject obj, float targetY)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            combinedBounds.Encapsulate(renderers[i].bounds);

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

    private void SafeDestroy(GameObject go)
    {
        if (go == null) return;
        if (Selection.activeGameObject != null)
        {
            if (Selection.activeGameObject == go || Selection.activeGameObject.transform.IsChildOf(go.transform))
                Selection.activeGameObject = null;
        }
        Undo.DestroyObjectImmediate(go);
    }

    private void UpdateParentReferences()
    {
        if (_tileParent == null) _tileParent = EnsureParent("MapEditor_Tiles");
        if (_edgeParent == null) _edgeParent = EnsureParent("MapEditor_Edges");
        if (_markerParent == null) _markerParent = EnsureParent("MapEditor_Markers");
    }

    private Transform EnsureParent(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go.transform;
    }

    private void EnsureBaseTile(GridCoords coords)
    {
        if (_context.GetTile(coords) == null) HandleCreateTile(coords);
    }

    private void ClearMap()
    {
        if (_tileParent != null) for (int i = _tileParent.childCount - 1; i >= 0; i--) SafeDestroy(_tileParent.GetChild(i).gameObject);
        if (_edgeParent != null) for (int i = _edgeParent.childCount - 1; i >= 0; i--) SafeDestroy(_edgeParent.GetChild(i).gameObject);
        // [New] 마커도 초기화
        if (_markerParent != null) for (int i = _markerParent.childCount - 1; i >= 0; i--) SafeDestroy(_markerParent.GetChild(i).gameObject);

        _context.InvalidateCache();
    }
}