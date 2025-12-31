using UnityEngine;

public class EditorWall : MonoBehaviour
{
    [Header("Identity")]
    public GridCoords Coordinate;
    public Direction Direction;

    [Header("Data")]
    public EdgeType Type;
    public CoverType Cover;
    public float MaxHP;
    public float CurrentHP;
    // EdgeDataType « µÂ ªË¡¶µ 

    public void Initialize(GridCoords coords, Direction dir, SavedEdgeInfo edgeInfo)
    {
        Coordinate = coords;
        Direction = dir;
        Type = edgeInfo.Type;
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
        if (!Application.isPlaying) UpdateName();
    }
}