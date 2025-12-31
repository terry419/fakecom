using UnityEngine;

public class EditorTile : MonoBehaviour
{
    [Header("Data")]
    public GridCoords Coordinate;
    public FloorType FloorID;
    public PillarType PillarID;

    public SavedEdgeInfo[] Edges = new SavedEdgeInfo[4];

    public void Initialize(GridCoords coords)
    {
        Coordinate = coords;
        // [Fix] Concrete -> Standard
        FloorID = FloorType.Standard;
        PillarID = PillarType.None;

        if (Edges == null || Edges.Length != 4) Edges = new SavedEdgeInfo[4];
        for (int i = 0; i < 4; i++) Edges[i] = SavedEdgeInfo.CreateOpen();

        SyncWithNeighboringWalls();
        UpdateName();
    }

    public void SyncWithNeighboringWalls()
    {
        EditorWall[] allWalls = FindObjectsOfType<EditorWall>();
        if (allWalls == null || allWalls.Length == 0) return;

        foreach (var wall in allWalls)
        {
            if (wall.Coordinate == this.Coordinate)
            {
                Edges[(int)wall.Direction] = CreateEdgeDataFromWall(wall);
                continue;
            }

            GridCoords wallFacingCoords = GridUtils.GetNeighbor(wall.Coordinate, wall.Direction);
            if (wallFacingCoords == this.Coordinate)
            {
                Direction myEdgeDir = GridUtils.GetOppositeDirection(wall.Direction);
                Edges[(int)myEdgeDir] = CreateEdgeDataFromWall(wall);
            }
        }
    }

    private SavedEdgeInfo CreateEdgeDataFromWall(EditorWall wall)
    {
        // DataType 제거된 메서드 호출
        switch (wall.Type)
        {
            case EdgeType.Wall: return SavedEdgeInfo.CreateWall(wall.MaxHP, wall.Cover);
            case EdgeType.Window: return SavedEdgeInfo.CreateWindow(wall.MaxHP, wall.Cover);
            case EdgeType.Door: return SavedEdgeInfo.CreateDoor(wall.MaxHP, wall.Cover);
            default: return SavedEdgeInfo.CreateOpen();
        }
    }

    public void UpdateName()
    {
        gameObject.name = $"Tile_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}";
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) UpdateName();
    }
}