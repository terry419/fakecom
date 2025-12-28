// [GDD 5.6] �׸��� �� Ÿ�� ���� (���� �� ���)
public enum CellType
{
    None = 0,   // ���/�߶�
    Floor,      // �ٴ� Ÿ��
    Pillar      // ���� ��� Ÿ�� (������ ��õ)
}

// [GDD 5.6] 4���� ���� (Axis Mapping: N=+Z, E=+X, S=-Z, W=-X)
public enum Direction
{
    North = 0, // +Z
    East = 1,  // +X
    South = 2, // -Z
    West = 3,   // -X
    None = -1
}