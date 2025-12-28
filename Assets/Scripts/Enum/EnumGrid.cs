// [GDD 5.6] 그리드 셀 타입 정의 (독립 셀 방식)
public enum CellType
{
    None = 0,   // 허공/추락
    Floor,      // 바닥 타일
    Pillar      // 독립 기둥 타일 (지지력 원천)
}

// [GDD 5.6] 4방향 정의 (Axis Mapping: N=+Z, E=+X, S=-Z, W=-X)
public enum Direction
{
    North = 0, // +Z
    East = 1,  // +X
    South = 2, // -Z
    West = 3   // -X
}