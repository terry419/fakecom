using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// This component is attached to wall/edge prefabs that are used in the editor.
/// It is "smart" and responsible for keeping its neighboring tiles' data in sync.
/// </summary>
public class EditorWall : MonoBehaviour
{
    [Header("Data")]
    public GridCoords Coordinate;
    [field: SerializeField]
    public Direction Direction { get; private set; }

    public EdgeType Type;
    public EdgeDataType DataType;
    public CoverType Cover;
    public float MaxHP;
    public float CurrentHP;

    public void Initialize(GridCoords coords, Direction dir, SavedEdgeInfo edgeInfo)
    {
        Coordinate = coords;
        Direction = dir;

        Type = edgeInfo.Type;
        DataType = edgeInfo.DataType;
        Cover = edgeInfo.Cover;
        MaxHP = edgeInfo.MaxHP;
        CurrentHP = edgeInfo.CurrentHP;

        UpdateName();
    }

    public void UpdateName()
    {
        gameObject.name = $"Edge_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}_{Direction}";
    }

    private void OnValidate()
    {
        UpdateName();
        EditorApplication.delayCall += () =>
        {
            // FIX: Check if the object has been destroyed before running delayed call
            if (this == null) return;
            UpdateNeighboringTiles(false);
        };
    }

    private void OnEnable()
    {
        EditorApplication.delayCall += () =>
        {
            // FIX: Check if the object has been destroyed before running delayed call
            if (this == null) return;
            UpdateNeighboringTiles(false);
        };
    }

    private void OnDestroy()
    {
        if (gameObject.scene.isLoaded && !Application.isPlaying)
        {
            UpdateNeighboringTiles(true);
        }
    }

    private void UpdateNeighboringTiles(bool isBeingDestroyed)
    {
        if (!gameObject.scene.IsValid() || Application.isPlaying) return;

        SavedEdgeInfo edgeDataForTile;
        if (isBeingDestroyed)
        {
            edgeDataForTile = SavedEdgeInfo.CreateOpen();
        }
        else
        {
            switch (Type)
            {
                case EdgeType.Wall: edgeDataForTile = SavedEdgeInfo.CreateWall(DataType, MaxHP, Cover); break;
                case EdgeType.Window: edgeDataForTile = SavedEdgeInfo.CreateWindow(DataType, MaxHP, Cover); break;
                case EdgeType.Door: edgeDataForTile = SavedEdgeInfo.CreateDoor(DataType, MaxHP, Cover); break;
                default: edgeDataForTile = SavedEdgeInfo.CreateOpen(); break;
            }
        }

        EditorTile fromTile = GetEditorTileAt(Coordinate);
        if (fromTile != null)
        {
            if (fromTile.Edges[(int)Direction].Type != edgeDataForTile.Type || fromTile.Edges[(int)Direction].DataType != edgeDataForTile.DataType)
            {
                Undo.RecordObject(fromTile, "Update Tile Edge from Wall");
                fromTile.Edges[(int)Direction] = edgeDataForTile;
                EditorUtility.SetDirty(fromTile);
            }
        }

        var (neighborCoords, oppositeDir) = GridUtils.GetOppositeEdge(Coordinate, Direction);
        EditorTile neighborTile = GetEditorTileAt(neighborCoords);
        if (neighborTile != null)
        {
            if (neighborTile.Edges[(int)oppositeDir].Type != edgeDataForTile.Type || neighborTile.Edges[(int)oppositeDir].DataType != edgeDataForTile.DataType)
            {
                Undo.RecordObject(neighborTile, "Update Tile Edge from Wall");
                neighborTile.Edges[(int)oppositeDir] = edgeDataForTile;
                EditorUtility.SetDirty(neighborTile);
            }
        }
    }

    private EditorTile GetEditorTileAt(GridCoords coords)
    {
        EditorTile[] allTiles = FindObjectsOfType<EditorTile>();
        foreach (var tile in allTiles)
        {
            if (tile.Coordinate == coords)
            {
                return tile;
            }
        }
        return null;
    }
}
