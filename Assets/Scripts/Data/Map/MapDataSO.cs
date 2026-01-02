using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Data/Map/MapData")]
public class MapDataSO : ScriptableObject
{
    [Header("1. Environment")]
    [Tooltip("전체 맵 크기")]
    public Vector2Int GridSize;
    [Tooltip("기준 좌표")]
    public Vector2Int BasePosition;

    public int MinLevel = 0;
    public int MaxLevel = 5;

    [Header("2. Tile Data")]
    public List<TileSaveData> Tiles = new List<TileSaveData>();

    // [Deprecated] SpawnPoints는 더 이상 사용하지 않음
    [HideInInspector]
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

        // RoleTag 중복 검증
        var roleTagSet = new HashSet<string>();
        foreach (var tile in Tiles)
        {
            if (string.IsNullOrEmpty(tile.RoleTag)) continue;

            if (roleTagSet.Contains(tile.RoleTag))
            {
                error = $"Duplicate RoleTag detected: '{tile.RoleTag}'. RoleTags must be unique per map.";
                return false;
            }
            roleTagSet.Add(tile.RoleTag);
        }

        error = string.Empty;
        return true;
    }

    // ========================================================================
    // [핵심] MissionManager가 태그로 위치를 찾기 위해 호출하는 함수
    // ========================================================================
    public bool TryGetRoleLocation(string roleTag, out Vector2Int coordinate)
    {
        coordinate = Vector2Int.zero;
        if (string.IsNullOrEmpty(roleTag) || Tiles == null) return false;

        foreach (var tile in Tiles)
        {
            if (tile.RoleTag == roleTag)
            {
                coordinate = new Vector2Int(tile.Coords.x, tile.Coords.z);
                return true;
            }
        }
        return false;
    }
}