using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This component is attached to tile prefabs that are used in the editor.
/// It holds the data for the tile before it's baked into the MapDataSO.
/// </summary>
public class EditorTile : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The grid coordinate of this tile.")]
    public GridCoords Coordinate;

    public FloorType FloorID;
    public SavedEdgeInfo[] Edges = new SavedEdgeInfo[4];

    /// <summary>
    /// Initializes the tile's data upon creation, and intelligently syncs with existing walls.
    /// </summary>
    public void Initialize(GridCoords coords)
    {
        Coordinate = coords;
        FloorID = FloorType.Concrete;

        // 1. First, set all edges to Open by default.
        if (Edges == null || Edges.Length != 4)
        {
            Edges = new SavedEdgeInfo[4];
        }
        for (int i = 0; i < 4; i++)
        {
            Edges[i] = SavedEdgeInfo.CreateOpen();
        }

        // 2. Then, check surroundings for existing walls and conform to them.
        SyncWithNeighboringWalls();

        UpdateName();
    }

    /// <summary>
    /// Finds any walls on the borders of this tile and updates this tile's edge data to match.
    /// </summary>
    private void SyncWithNeighboringWalls()
    {
        // This can be slow, but it only runs once on creation in the editor, which is acceptable for correctness.
        EditorWall[] allWalls = FindObjectsOfType<EditorWall>();

        foreach (var wall in allWalls)
        {
            // Check if the wall belongs on one of our edges (from our perspective)
            if (wall.Coordinate == this.Coordinate)
            {
                int dirIndex = (int)wall.Direction;
                if (dirIndex >= 0 && dirIndex < 4)
                {
                    Edges[dirIndex] = CreateEdgeDataFromWall(wall);
                }
            }

            // Check if the wall belongs on one of our edges (from the neighbor's perspective)
            var (neighborCoords, oppositeDir) = GridUtils.GetOppositeEdge(this.Coordinate, wall.Direction);
            if (wall.Coordinate == neighborCoords)
            {
                int dirIndex = (int)GridUtils.GetOppositeDirection(wall.Direction);
                if (dirIndex >= 0 && dirIndex < 4)
                {
                    Edges[dirIndex] = CreateEdgeDataFromWall(wall);
                }
            }
        }
    }

    private SavedEdgeInfo CreateEdgeDataFromWall(EditorWall wall)
    {
        switch (wall.Type)
        {
            case EdgeType.Wall:
                return SavedEdgeInfo.CreateWall(wall.DataType, wall.MaxHP, wall.Cover);
            case EdgeType.Window:
                return SavedEdgeInfo.CreateWindow(wall.DataType, wall.MaxHP, wall.Cover);
            case EdgeType.Door:
                return SavedEdgeInfo.CreateDoor(wall.DataType, wall.MaxHP, wall.Cover);
            default:
                return SavedEdgeInfo.CreateOpen();
        }
    }

    /// <summary>
    /// A helper to easily update the object's name based on its coordinates.
    /// </summary>
    public void UpdateName()
    {
        gameObject.name = $"Tile_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}";
    }

    private void OnValidate()
    {
        // When coordinates are changed in the Inspector, update the name automatically.
        UpdateName();
    }
}
