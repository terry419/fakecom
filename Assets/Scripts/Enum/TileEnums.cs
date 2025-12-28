// ���: Assets/Scripts/Enum/TileEnums.cs

public enum FloorType
{
    None = 0,       // ������ ����/����
    Void,           // ��� (�߶� ����)
    Concrete,       // ��ũ��Ʈ �ٴ�
    Dirt,           // ����
    Metal,          // �ݼ�
    Grass           // Ǯ
}

public enum PillarType
{
    None = 0,       // ������ ����
    Empty,          // ��� ����
    Concrete,       // ��ũ��Ʈ ���
    Steel,          // ö�� ���
    Marble          // ��Ŀ� ���
}

public enum EdgeType
{
    Unknown = 0,    // �ʱ�ȭ �ȵ�
    Open,           // ������ ����� (�̵� ����)
    Wall,           // �� (�̵�/�þ� ����)
    Window,         // â�� (�þ� ����, �̵� �Ұ�)
    Door            // �� (��ȣ�ۿ� ����)
}

public enum CoverType
{
    None = 0,       // ���� ���� (0%)
    Low,            // �ݾ��� (20% ~)
    High            // �������� (40% ~)
}

public enum EdgeDataType
{
    None = 0,
    Concrete,
    Brick,
    Steel,
    Glass,
    Wood
}

// [����] None(0) ����. ��� ITileOccupant�� �ݵ�� ��ȿ�� Ÿ���� ������ ��.
public enum OccupantType
{
    Unit = 1,       // ���� (Primary Occupant)
    Item = 2,       // ������
    Obstacle = 3,   // ��ֹ�
    Trap = 4        // ����
}