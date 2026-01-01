using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Data/Map/MapData")]
public class MapDataSO : ScriptableObject
{
    // [문제 1 해결] MapID 삭제. 이제 메타데이터(MapEntry)가 ID를 전담합니다.
    // public string MapID; (Deleted)

    [Header("1. Environment")]
    [Tooltip("전체 맵 크기")]
    public Vector2Int GridSize;
    [Tooltip("기준 좌표")]
    public Vector2Int BasePosition;

    public int MinLevel = 0;
    public int MaxLevel = 5;

    [Header("2. Tile Data")]
    public List<TileSaveData> Tiles = new List<TileSaveData>();
    public List<SpawnPointData> SpawnPoints;

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