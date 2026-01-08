using UnityEngine;
using UnityEditor;
using System;

public class MapEditorSceneInput
{
    private readonly MapEditorContext _context;

    // 타일/벽 관련 이벤트
    public event Action<GridCoords> OnCreateTileRequested;
    public event Action<GridCoords, Direction> OnModifyEdgeRequested;
    public event Action<GridCoords> OnCreatePillarRequested;
    public event Action<GridCoords> OnEraseTileRequested;

    // [New] 마커 관련 이벤트 (분리됨)
    public event Action<GridCoords> OnCreatePortalRequested;
    public event Action<GridCoords> OnCreateSpawnRequested;

    // [New] 마커 삭제 이벤트 (공용)
    public event Action<GridCoords> OnRemoveMarkerRequested;

    public MapEditorSceneInput(MapEditorContext context)
    {
        _context = context;
    }

    public void HandleSceneGUI(SceneView sceneView)
    {
        HandleMouseInput();
        DrawVisuals();
    }

    private void HandleMouseInput()
    {
        Event guiEvent = Event.current;
        if (guiEvent.type == EventType.Layout || guiEvent.type == EventType.Repaint) return;

        // 클릭 제어권 확보 (오브젝트 선택 방지)
        if (_context.CurrentToolMode != MapEditorContext.ToolMode.Tile) // Tile 모드가 아닐 때도 동작하도록
        {
            int controlID = GUIUtility.GetControlID(FocusType.Passive);
            HandleUtility.AddDefaultControl(controlID);
        }

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, _context.CurrentLevel * GridUtils.LEVEL_HEIGHT, 0));

        if (gridPlane.Raycast(mouseRay, out float distance))
        {
            Vector3 worldPos = mouseRay.GetPoint(distance);
            _context.MouseGridCoords = GridUtils.WorldToGrid(worldPos);
            _context.MouseGridCoords.y = _context.CurrentLevel;
            _context.IsMouseOverGrid = true;

            // Edge 모드일 때만 엣지 계산
            if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge)
                CalculateHighlightedEdge(worldPos);
            else
                _context.HighlightedEdge.SetInvalid();

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0)
            {
                DispatchClickEvent(guiEvent);
                guiEvent.Use();
            }
        }
        else
        {
            _context.IsMouseOverGrid = false;
            _context.HighlightedEdge.SetInvalid();
        }
    }

    private void CalculateHighlightedEdge(Vector3 mouseWorldPos)
    {
        GridCoords tileCoords = _context.MouseGridCoords;
        Vector3 tileCenter = GridUtils.GridToWorld(tileCoords);
        Vector3 offset = mouseWorldPos - tileCenter;

        float threshold = GridUtils.CELL_SIZE * 0.35f;
        // 중앙 부근이면 엣지 선택 안 함
        if (Mathf.Abs(offset.x) < threshold && Mathf.Abs(offset.z) < threshold)
        {
            _context.HighlightedEdge.SetInvalid();
            return;
        }

        Direction dir;
        if (Mathf.Abs(offset.x) > Mathf.Abs(offset.z))
            dir = offset.x > 0 ? Direction.East : Direction.West;
        else
            dir = offset.z > 0 ? Direction.North : Direction.South;

        Vector3 edgePos = GridUtils.GetEdgeWorldPosition(tileCoords, dir);
        _context.HighlightedEdge.Set(tileCoords, dir, edgePos, true);
    }

    private void DispatchClickEvent(Event guiEvent)
    {
        bool isAlt = guiEvent.alt;

        switch (_context.CurrentToolMode)
        {
            case MapEditorContext.ToolMode.Tile:
                if (!isAlt) OnCreateTileRequested?.Invoke(_context.MouseGridCoords);
                break;

            case MapEditorContext.ToolMode.Edge:
                if (_context.HighlightedEdge.IsValid && !isAlt)
                    OnModifyEdgeRequested?.Invoke(_context.HighlightedEdge.Tile, _context.HighlightedEdge.Dir);
                break;

            case MapEditorContext.ToolMode.Pillar:
                if (!isAlt) OnCreatePillarRequested?.Invoke(_context.MouseGridCoords);
                break;

            case MapEditorContext.ToolMode.Erase:
                OnEraseTileRequested?.Invoke(_context.MouseGridCoords);
                break;

            // [New] Portal Mode Logic
            case MapEditorContext.ToolMode.Portal:
                if (isAlt)
                    OnRemoveMarkerRequested?.Invoke(_context.MouseGridCoords);
                else
                    OnCreatePortalRequested?.Invoke(_context.MouseGridCoords);
                break;

            // [New] Spawn Mode Logic
            case MapEditorContext.ToolMode.Spawn:
                if (isAlt)
                    OnRemoveMarkerRequested?.Invoke(_context.MouseGridCoords);
                else
                    OnCreateSpawnRequested?.Invoke(_context.MouseGridCoords);
                break;
        }
    }

    private void DrawVisuals()
    {
        if (!_context.IsMouseOverGrid) return;

        Color c = Color.cyan; // Default

        // 모드별 커서 색상 변경 (직관성 강화)
        switch (_context.CurrentToolMode)
        {
            case MapEditorContext.ToolMode.Pillar: c = Color.yellow; break;
            case MapEditorContext.ToolMode.Erase: c = Color.red; break;
            case MapEditorContext.ToolMode.Portal:
                c = (_context.SelectedPortalType == PortalType.In) ? new Color(0.6f, 0, 1f) : Color.blue;
                if (Event.current.alt) c = Color.red; // 삭제 모드일 땐 빨강
                break;
            case MapEditorContext.ToolMode.Spawn:
                c = (_context.SelectedSpawnType == MarkerType.PlayerSpawn) ? Color.green : new Color(1f, 0.5f, 0f);
                if (Event.current.alt) c = Color.red;
                break;
        }

        DrawWireCube(GridUtils.GridToWorld(_context.MouseGridCoords), new Vector3(GridUtils.CELL_SIZE, 0.1f, GridUtils.CELL_SIZE), c);

        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Edge && _context.HighlightedEdge.IsValid)
        {
            DrawEdgeHighlight(_context.HighlightedEdge.WorldPos, _context.HighlightedEdge.Dir, Color.magenta);
        }
    }

    private void DrawWireCube(Vector3 center, Vector3 size, Color color)
    {
        Color old = Handles.color;
        Handles.color = color;
        Handles.DrawWireCube(center, size);
        Handles.color = old;
    }

    private void DrawEdgeHighlight(Vector3 center, Direction dir, Color color)
    {
        Color old = Handles.color;
        Handles.color = color;

        Vector3 size;
        if (dir == Direction.North || dir == Direction.South)
            size = new Vector3(GridUtils.CELL_SIZE, 1.5f, 0.1f);
        else
            size = new Vector3(0.1f, 1.5f, GridUtils.CELL_SIZE);

        Handles.DrawWireCube(center + Vector3.up * 0.75f, size);
        Handles.color = old;
    }
}