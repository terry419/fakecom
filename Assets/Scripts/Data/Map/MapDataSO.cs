using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewMapData", menuName = "YCOM/Data/MapData")]
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
    // 기존 구조 유지: TileSaveData 안에 RoleTag가 포함되어 있음
    public List<TileSaveData> Tiles = new List<TileSaveData>();

    // [Validation] 기존 로직 유지 (중복 태그 검사 포함)
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

        // RoleTag 중복 검증 (필수)
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
    // [추가] MissionManager가 사용할 좌표 조회 메서드
    // ========================================================================

    /// <summary>
    /// 특정 RoleTag를 가진 타일의 GridCoords를 반환합니다.
    /// </summary>
    public bool TryGetTaggedCoords(string tag, out GridCoords coords)
    {
        coords = default;
        if (string.IsNullOrEmpty(tag) || Tiles == null) return false;

        // Tiles 리스트에서 RoleTag가 일치하는 녀석을 찾음
        foreach (var tile in Tiles)
        {
            if (tile.RoleTag == tag)
            {
                // TileSaveData의 좌표(Vector3Int)를 GridCoords로 변환
                coords = new GridCoords(tile.Coords.x, tile.Coords.z, tile.Coords.y);
                return true;
            }
        }
        return false;
    }
}