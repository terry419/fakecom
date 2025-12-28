// 경로: Assets/Scripts/Enum/TileEnums.cs

public enum FloorType
{
    None = 0,       // 데이터 없음/오류
    Void,           // 허공 (추락 판정)
    Concrete,       // 콘크리트 바닥
    Dirt,           // 진흙
    Metal,          // 금속
    Grass           // 풀
}

public enum PillarType
{
    None = 0,       // 데이터 없음
    Empty,          // 기둥 없음
    Concrete,       // 콘크리트 기둥
    Steel,          // 철골 기둥
    Marble          // 장식용 기둥
}

public enum EdgeType
{
    Unknown = 0,    // 초기화 안됨
    Open,           // 완전히 개방됨 (이동 가능)
    Wall,           // 벽 (이동/시야 차단)
    Window,         // 창문 (시야 투과, 이동 불가)
    Door            // 문 (상호작용 가능)
}

public enum CoverType
{
    None = 0,       // 엄폐 없음 (0%)
    Low,            // 반엄폐 (20% ~)
    High            // 완전엄폐 (40% ~)
}

public enum EdgeDataType
{
    None = 0,
    ConcreteWall,
    BrickWall,
    SteelFence,
    GlassWindow,
    WoodDoor
}

// [수정] None(0) 제거. 모든 ITileOccupant는 반드시 유효한 타입을 가져야 함.
public enum OccupantType
{
    Unit = 1,       // 유닛 (Primary Occupant)
    Item = 2,       // 아이템
    Obstacle = 3,   // 장애물
    Trap = 4        // 함정
}