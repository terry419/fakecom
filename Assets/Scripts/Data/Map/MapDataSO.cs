// 파일: Assets/Scripts/Data/Map/MapDataSO.cs
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

    // [Deprecated] SpawnPoints는 더 이상 사용하지 않음 (RoleTag로 대체)
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

        // [New] RoleTag 중복 검증 필수
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

    // [New] 메서드명 명확화: 유일한 Role 위치 반환
    public bool TryGetRoleLocation(string roleTag, out Vector2Int coordinate)
    {
        coordinate = Vector2Int.zero;
        if (string.IsNullOrEmpty(roleTag)) return false;

        // 리스트 순회 (RoleTag는 유일하므로 찾으면 즉시 반환)
        foreach (var tile in Tiles)
        {
            if (tile.RoleTag == roleTag)
            {
                // [주석 명확화] GridCoords는 3D (x, y, z)인데, y는 높이이므로 무시하고 xz 평면 좌표만 사용
                coordinate = new Vector2Int(tile.Coords.x, tile.Coords.z);
                return true;
            }
        }
        return false;
    }
}