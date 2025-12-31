using UnityEngine;
using UnityEditor;
using System;

// [Fix] static 제거 -> 인스턴스 생성 가능
public class MapEditorSceneInput
{
    private readonly MapEditorContext _context;

    public event Action<GridCoords> OnCreateTileRequested;
    public event Action<GridCoords, Direction> OnModifyEdgeRequested;
    public event Action<GridCoords> OnCreatePillarRequested;
    public event Action<GridCoords> OnEraseTileRequested;

    public MapEditorSceneInput(MapEditorContext context)
    {
        _context = context;
    }

    public void HandleSceneGUI(SceneView sceneView)
    {
        if (_context.Settings == null) return;
        HandleMouseInput();
        DrawVisuals();
    }

    private void HandleMouseInput()
    {
        Event guiEvent = Event.current;
        // 마우스 이벤트가 아니면 무시 (레이아웃 에러 방지)
        if (guiEvent.type == EventType.Layout || guiEvent.type == EventType.Repaint) return;

        int controlID = GUIUtility.GetControlID(FocusType.Passive);
        HandleUtility.AddDefaultControl(controlID);

        Ray mouseRay = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
        // Y축 평면 (현재 레벨 기준)
        Plane gridPlane = new Plane(Vector3.up, new Vector3(0, _context.CurrentLevel * GridUtils.LEVEL_HEIGHT, 0));

        if (gridPlane.Raycast(mouseRay, out float distance))
        {
            Vector3 worldPos = mouseRay.GetPoint(distance);
            _context.MouseGridCoords = GridUtils.WorldToGrid(worldPos);
            _context.MouseGridCoords.y = _context.CurrentLevel;
            _context.IsMouseOverGrid = true;

            CalculateHighlightedEdge(worldPos);

            if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && !guiEvent.alt)
            {
                DispatchClickEvent();
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

        // 타일 중앙에 가까우면 엣지 선택 안 함
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

        // [Fix] Set 메서드 인자 4개 (Context 정의와 일치시킴)
        _context.HighlightedEdge.Set(tileCoords, dir, edgePos, true);
    }

    private void DispatchClickEvent()
    {
        switch (_context.CurrentToolMode)
        {
            case MapEditorContext.ToolMode.Tile:
                OnCreateTileRequested?.Invoke(_context.MouseGridCoords);
                break;
            case MapEditorContext.ToolMode.Edge:
                // [Fix] IsValid는 프로퍼티이므로 () 제거
                if (_context.HighlightedEdge.IsValid)
                    OnModifyEdgeRequested?.Invoke(_context.HighlightedEdge.Tile, _context.HighlightedEdge.Dir);
                break;
            case MapEditorContext.ToolMode.Pillar:
                OnCreatePillarRequested?.Invoke(_context.MouseGridCoords);
                break;
            case MapEditorContext.ToolMode.Erase:
                OnEraseTileRequested?.Invoke(_context.MouseGridCoords);
                break;
        }
    }

    private void DrawVisuals()
    {
        if (!_context.IsMouseOverGrid) return;

        Color c = Color.cyan;
        if (_context.CurrentToolMode == MapEditorContext.ToolMode.Pillar) c = Color.yellow;
        else if (_context.CurrentToolMode == MapEditorContext.ToolMode.Erase) c = Color.red;

        DrawWireCube(GridUtils.GridToWorld(_context.MouseGridCoords), new Vector3(GridUtils.CELL_SIZE, 0.1f, GridUtils.CELL_SIZE), c);

        // [Fix] IsValid 프로퍼티 사용
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
        float t = 0.1f;
        Vector3 s = (dir == Direction.North || dir == Direction.South)
            ? new Vector3(GridUtils.CELL_SIZE, 0.2f, t)
            : new Vector3(t, 0.2f, GridUtils.CELL_SIZE);
        DrawWireCube(center, s, color);
    }
}