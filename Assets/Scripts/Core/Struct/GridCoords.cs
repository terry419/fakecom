using System;
using UnityEngine;

/// <summary>
/// [GDD 5.6] 3D 정수 좌표계.
/// 순서: X(Col), Z(Row), Y(Level).
/// </summary>
[Serializable]
public struct GridCoords : IEquatable<GridCoords>, IComparable<GridCoords>
{
    public int x; // Column (East/West)
    public int z; // Row (North/South)
    public int y; // Level (Height)

    // 생성자 순서 통일: (x, z, y)
    public GridCoords(int x, int z, int y)
    {
        this.x = x;
        this.z = z;
        this.y = y;
    }

    // Unity 호환 생성자 (Vector3Int의 y를 높이로 인식)
    public GridCoords(Vector3Int vec) : this(vec.x, vec.z, vec.y) { }

    // 연산자 오버로딩
    public static GridCoords operator +(GridCoords a, GridCoords b) => new GridCoords(a.x + b.x, a.z + b.z, a.y + b.y);
    public static GridCoords operator -(GridCoords a, GridCoords b) => new GridCoords(a.x - b.x, a.z - b.z, a.y - b.y);
    public static bool operator ==(GridCoords a, GridCoords b) => a.x == b.x && a.z == b.z && a.y == b.y;
    public static bool operator !=(GridCoords a, GridCoords b) => !(a == b);

    // Dictionary Key 필수 구현
    public override bool Equals(object obj) => obj is GridCoords other && Equals(other);
    public bool Equals(GridCoords other) => x == other.x && z == other.z && y == other.y;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + x.GetHashCode();
            hash = hash * 23 + z.GetHashCode();
            hash = hash * 23 + y.GetHashCode();
            return hash;
        }
    }

    // [수정] 필드 순서(x, z, y)와 일치하는 출력 포맷
    public override string ToString() => $"({x}, {z}, {y})";

    // [수정] 데이터 손상 방지를 위해 순서 일치 (x, z, y)
    public Vector3Int ToVector3Int() => new Vector3Int(x, z, y);

    // [추가] 정렬/비교용 구현 (Level -> Row -> Col 순 우선순위)
    public int CompareTo(GridCoords other)
    {
        if (y != other.y) return y.CompareTo(other.y);
        if (z != other.z) return z.CompareTo(other.z);
        return x.CompareTo(other.x);
    }
}