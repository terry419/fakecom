using UnityEngine;

public class EditorPillar : MonoBehaviour
{
    [Header("Data")]
    public GridCoords Coordinate;
    public PillarType PillarID;

    public void UpdateName()
    {
        gameObject.name = $"Pillar_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}";
    }

    private void OnValidate()
    {
        UpdateName();
    }

    public void Initialize(GridCoords coords)
    {
        Coordinate = coords;
        // [Fix] Concrete -> Standing (변경된 Enum 반영)
        PillarID = PillarType.Standing;
        UpdateName();
    }
}