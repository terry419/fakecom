using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 경로 계산 결과를 캡슐화한 불변 객체 (VO)
/// Null Safety를 보장하며, 유효 경로와 불가 경로를 명확히 구분함.
/// </summary>
public class PathCalculationResult
{
    public readonly IReadOnlyList<GridCoords> ValidPath;
    public readonly IReadOnlyList<GridCoords> InvalidPath;
    public readonly bool IsBlocked;
    public readonly int RequiredAP;
    public readonly bool HasPath;

    private PathCalculationResult(List<GridCoords> valid, List<GridCoords> invalid, bool blocked, int ap)
    {
        ValidPath = valid ?? new List<GridCoords>();
        InvalidPath = invalid ?? new List<GridCoords>();
        IsBlocked = blocked;
        RequiredAP = ap;
        HasPath = ValidPath.Count > 0 || InvalidPath.Count > 0;
    }

    // Factory Method for Success/Partial
    public static PathCalculationResult Create(List<GridCoords> valid, List<GridCoords> invalid, bool blocked, int ap)
    {
        return new PathCalculationResult(valid, invalid, blocked, ap);
    }

    // Factory Method for Empty/Fail
    public static PathCalculationResult Empty => new PathCalculationResult(new List<GridCoords>(), new List<GridCoords>(), false, 0);
}