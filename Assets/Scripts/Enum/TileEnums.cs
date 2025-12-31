public enum FloorType
{
    None = 0,
    Void,
    Standard,
    Damaged,
    Broken
}

public enum PillarType
{
    None = 0,
    Standing,
    Broken,
    Debris
}

public enum EdgeType
{
    Open = 0,
    Wall,
    Window,
    Door,
    fence
}

public enum CoverType
{
    None = 0,
    Low,
    High
}

public enum OccupantType
{
    Unit = 1,
    Item = 2,
    Obstacle = 3,
    Trap = 4
}
// EdgeDataType은 삭제됨