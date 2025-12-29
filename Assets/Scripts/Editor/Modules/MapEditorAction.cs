using UnityEngine;
using UnityEditor;

public class MapEditorAction
{
    private readonly MapEditorContext _context;
    private Transform _tileParent;

    public MapEditorAction(MapEditorContext context)
    {
        _context = context;
        UpdateTileParentReference();
    }

    // --- Public Handlers ---

    public void HandleCreateTile(GridCoords coords)
    {
        // ... (기존 타일 생성 로직 동일) ...
        if (_context.Settings.DefaultTilePrefab == null) return;
        if (_context.GetTile(coords) != null) return;

        UpdateTileParentReference();
        GameObject newTileObj = (GameObject)PrefabUtility.InstantiatePrefab(_context.Settings.DefaultTilePrefab, _tileParent);
        Undo.RegisterCreatedObjectUndo(newTileObj, "Create Tile");

        newTileObj.transform.position = GridUtils.GridToWorld(coords);
        newTileObj.name = $"Tile_{coords.x}_{coords.z}_{coords.y}";

        var editorTile = newTileObj.GetComponent<EditorTile>();
        if (editorTile != null) editorTile.Initialize(coords);

        _context.InvalidateCache();
    }

    // [Fix] 기둥 생성 핸들러
    public void HandleCreatePillar(GridCoords coords)
    {
        EditorTile tile = _context.GetTile(coords);
        if (tile == null)
        {
            // 타일이 없으면 타일을 먼저 생성
            HandleCreateTile(coords);
            tile = _context.GetTile(coords); // 다시 가져오기
        }

        if (tile != null)
        {
            Undo.RecordObject(tile, "Set Pillar");
            tile.PillarID = _context.SelectedPillarType;
            EditorUtility.SetDirty(tile);

            // 시각적 기둥 객체 생성 (기존 기둥이 있다면 제거 후 생성)
            RebuildPillarVisual(tile);
        }
    }

    public void HandleModifyEdge(GridCoords coords, Direction dir)
    {
        // ... (기존 엣지 로직 동일) ...
        var newEdgeData = CreateEdgeDataFromContext();
        SyncEdgeData(coords, dir, newEdgeData);
        ReplaceEdgeVisuals(coords, dir, newEdgeData);
        _context.InvalidateCache();
    }

    public void HandleEraseTile(GridCoords coords)
    {
        var tile = _context.GetTile(coords);
        if (tile != null)
        {
            Undo.DestroyObjectImmediate(tile.gameObject);
        }

        // 연관된 벽 제거
        for (int i = 0; i < 4; i++)
        {
            Direction dir = (Direction)i;
            var (neighbor, opposite) = GridUtils.GetOppositeEdge(coords, dir);

            EditorWall w1 = _context.GetWall(coords, dir);
            EditorWall w2 = _context.GetWall(neighbor, opposite);

            if (w1 != null) Undo.DestroyObjectImmediate(w1.gameObject);
            if (w2 != null) Undo.DestroyObjectImmediate(w2.gameObject);
        }

        _context.InvalidateCache();
    }

    public void LoadMapFromData(MapDataSO data)
    {
        // ... (기존 Load 로직 + 기둥 복원 로직 추가) ...
        UpdateTileParentReference();
        ClearMap();

        Undo.SetCurrentGroupName("Load Map");
        int group = Undo.GetCurrentGroup();
        Transform edgeParent = GetOrCreateEdgeParent();

        foreach (var tileData in data.Tiles)
        {
            // 타일 생성
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
                editorTile.PillarID = tileData.PillarID; // [Fix] 기둥 데이터 복원
                editorTile.Edges = tileData.Edges;

                // 기둥 시각화 복원
                if (editorTile.PillarID != PillarType.None)
                {
                    RebuildPillarVisual(editorTile);
                }
            }

            // 벽 생성 Loop ... (기존 동일)
            for (int i = 0; i < 4; i++)
            {
                if (tileData.Edges[i].Type == EdgeType.Open) continue;
                Direction dir = (Direction)i;
                GameObject wallPrefab = GetPrefabForEdgeType(tileData.Edges[i].Type);
                if (wallPrefab == null) continue;

                var wallObj = (GameObject)PrefabUtility.InstantiatePrefab(wallPrefab, edgeParent);
                Undo.RegisterCreatedObjectUndo(wallObj, "Load Wall");
                // ... 위치/회전 설정 및 Initialize ...
                Vector3 edgePos = GridUtils.GetEdgeWorldPosition(tileData.Coords, dir);
                wallObj.transform.position = edgePos + new Vector3(0, GridUtils.LEVEL_HEIGHT / 2.0f, 0);
                wallObj.transform.rotation = (dir == Direction.North || dir == Direction.South) ? Quaternion.identity : Quaternion.Euler(0, 90, 0);

                var editorWall = wallObj.GetComponent<EditorWall>();
                if (editorWall != null) editorWall.Initialize(tileData.Coords, dir, tileData.Edges[i]);
            }
        }
        Undo.CollapseUndoOperations(group);
        _context.InvalidateCache();
    }

    // --- Helpers ---

    // [Fix] 기둥 비주얼 생성 로직
    private void RebuildPillarVisual(EditorTile tile)
    {
        // 기존 기둥 비주얼 제거 (자식 중 이름으로 찾거나 태그로 관리)
        Transform existingPillar = tile.transform.Find("Visual_Pillar");
        if (existingPillar != null) Undo.DestroyObjectImmediate(existingPillar.gameObject);

        if (tile.PillarID == PillarType.None) return;

        // Settings에서 프리팹 찾기 (SettingsSO에 PillarTable이 있다고 가정)
        GameObject pillarPrefab = GetPrefabForPillarType(tile.PillarID);
        if (pillarPrefab != null)
        {
            GameObject pillarObj = (GameObject)PrefabUtility.InstantiatePrefab(pillarPrefab, tile.transform);
            Undo.RegisterCreatedObjectUndo(pillarObj, "Create Pillar Visual");
            pillarObj.name = "Visual_Pillar";
            pillarObj.transform.localPosition = Vector3.zero; // 타일 중앙
        }
    }

    private GameObject GetPrefabForPillarType(PillarType type)
    {
        // GDD 14.4에 따라 MapEditorSettingsSO가 PillarTable을 가짐
        // 실제 구현 시 SettingsSO에 매핑 로직 필요
        // 임시로 DefaultPillarPrefab 반환 (실제로는 type에 따라 switch)
        return _context.Settings.DefaultPillarPrefab;
    }

    // ... (나머지 ClearMap, SyncEdgeData 등 기존 동일) ...
    private void ClearMap() { /* ... */ }
    private void UpdateTileParentReference() { /* ... */ }
    private Transform GetOrCreateEdgeParent() { /* ... */ return null; } // 생략(기존 코드 참조)
    private SavedEdgeInfo CreateEdgeDataFromContext() { /* ... */ return default; } // 생략
    private void SyncEdgeData(GridCoords c, Direction d, SavedEdgeInfo e) { /* ... */ }
    private void ReplaceEdgeVisuals(GridCoords c, Direction d, SavedEdgeInfo e) { /* ... */ }
    private GameObject GetPrefabForEdgeType(EdgeType t) { return null; } // 생략
}