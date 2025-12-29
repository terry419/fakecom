using System;
using UnityEngine;

/// <summary>
/// [GDD 5.6] 3D 정수 좌표계 구조체.
/// </summary>
[Serializable]
public struct GridCoords : IEquatable<GridCoords>, IComparable<GridCoords>
{
    // [리팩토링 4] 좌표계 의미 명확화

    /// <summary>
    /// Column (East/West). 2D 그리드의 가로축, 3D 월드의 X축입니다.
    /// </summary>
    public int x;

    /// <summary>
    /// Row (North/South). 2D 그리드의 세로축, 3D 월드의 Z축(Depth)입니다.
    /// </summary>
    public int z;

    /// <summary>
    /// Level (Height). 층수를 나타내며, 3D 월드의 Y축입니다.
    /// 로직에서는 주로 'levelIndex' 등으로 변환되어 사용됩니다.
    /// </summary>
    public int y;

    public GridCoords(int x, int z, int y)
    {
        this.x = x;
        this.z = z;
        this.y = y;
    }

    public GridCoords(Vector3Int vec) : this(vec.x, vec.z, vec.y) { }

    public static GridCoords operator +(GridCoords a, GridCoords b) => new GridCoords(a.x + b.x, a.z + b.z, a.y + b.y);
    public static GridCoords operator -(GridCoords a, GridCoords b) => new GridCoords(a.x - b.x, a.z - b.z, a.y - b.y);
    public static bool operator ==(GridCoords a, GridCoords b) => a.x == b.x && a.z == b.z && a.y == b.y;
    public static bool operator !=(GridCoords a, GridCoords b) => !(a == b);

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

    public override string ToString() => $"({x}, {z}, {y})";

    public Vector3Int ToVector3Int() => new Vector3Int(x, z, y);

    public int CompareTo(GridCoords other)
    {
        // 정렬 우선순위: 층(Y) -> 행(Z) -> 열(X)
        if (y != other.y) return y.CompareTo(other.y);
        if (z != other.z) return z.CompareTo(other.z);
        return x.CompareTo(other.x);
    }
}