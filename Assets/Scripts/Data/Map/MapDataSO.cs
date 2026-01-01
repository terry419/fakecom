using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Data/Map/MapData")]
public class MapDataSO : ScriptableObject
{
    [Header("Debug Info")]
    public string MapID;

    [Header("Map Structure")]
    public Vector2Int GridSize;
    public Vector2Int BasePosition; // [Fix] MapManager 호환성 복구
    public int MinLevel = 0;        // [Fix] MapManager 호환성 복구
    public int MaxLevel = 5;        // [Fix] MapManager 호환성 복구

    [Header("Content")]
    public List<TileSaveData> Tiles = new List<TileSaveData>();
    public List<SpawnPointData> SpawnPoints;

    // [개선 5] bool Validate() 로 변경
    public bool Validate(out string error)
    {
        if (Tiles == null || Tiles.Count == 0)
        {
            error = $"[MapDataSO] {name} has no tiles!";
            return false;
        }
        if (GridSize.x <= 0 || GridSize.y <= 0)
        {
            error = $"[MapDataSO] {name} has invalid GridSize: {GridSize}";
            return false;
        }

        error = string.Empty;
        return true;
    }
}