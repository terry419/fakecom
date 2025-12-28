using UnityEngine;

/// <summary>
/// This component is attached to pillar prefabs that are used in the editor.
/// It holds the data for the pillar before it's baked into the MapDataSO.
/// </summary>
public class EditorPillar : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("The grid coordinate of this pillar.")]
    public GridCoords Coordinate;
    public PillarType PillarID;

    /// <summary>
    /// A helper to easily update the object's name based on its coordinates.
    /// </summary>
    public void UpdateName()
    {
        gameObject.name = $"Pillar_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}";
    }

    private void OnValidate()
    {
        // When coordinates are changed in the Inspector, update the name automatically.
        UpdateName();
    }

    public void Initialize(GridCoords coords)
    {
        Coordinate = coords;
        PillarID = PillarType.Concrete; // Default to Concrete
        UpdateName();
    }
}
