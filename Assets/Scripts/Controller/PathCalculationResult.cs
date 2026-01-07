using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 이동 경로 계산 결과를 담는 불변(Immutable) 객체입니다.
/// 유닛이 실제로 이동할 수 있는 경로(Valid)와,
/// 시각적으로만 표시해 주어야 하는 경로(Invalid, 이동력 부족 혹은 장애물 등)를 분리하여 관리합니다.
/// </summary>
public class PathCalculationResult
{
    // --------------------------------------------------------------------------
    // 1. Singleton Empty Instance (메모리 할당 최적화)
    // --------------------------------------------------------------------------
    /// <summary>
    /// 경로가 없는 빈 상태를 나타내는 싱글턴 객체입니다.
    /// </summary>
    public static PathCalculationResult Empty { get; } = new PathCalculationResult(null, null, false, 0);

    // --------------------------------------------------------------------------
    // 2. Data Fields (읽기 전용)
    // --------------------------------------------------------------------------
    /// <summary>
    /// 실제로 이동 가능한 경로입니다. (Mobility 범위 내, 장애물 없음)
    /// </summary>
    public IReadOnlyList<GridCoords> ValidPath { get; }

    /// <summary>
    /// 이동은 불가능하지만 시각적으로 표시해줘야 하는 경로입니다. (Mobility 초과, 혹은 장애물 경로)
    /// </summary>
    public IReadOnlyList<GridCoords> InvalidPath { get; }

    /// <summary>
    /// 경로가 장애물(유닛, 벽 등)에 의해 막혔는지 여부입니다.
    /// </summary>
    public bool IsBlocked { get; }

    /// <summary>
    /// 이 경로(ValidPath)를 이동하는 데 소모되는 Mobility 값입니다.
    /// </summary>
    public int RequiredMobility { get; }

    // --------------------------------------------------------------------------
    // 3. Helper Properties (컨트롤러 제공용)
    // --------------------------------------------------------------------------

    /// <summary>
    /// 어떤 경로든 경로(유효/무효)가 존재하는지 여부.
    /// </summary>
    public bool HasAnyPath => ValidPath.Count > 0 || InvalidPath.Count > 0;

    /// <summary>
    /// 실제로 이동 가능한 '완전 유효한' 경로인지 여부.
    /// (유효 경로가 존재하며, 장애물에 막히지 않음)
    /// </summary>
    public bool IsValidMovePath => ValidPath.Count > 0 && !IsBlocked;

    /// <summary>
    /// 유효 경로가 존재하지만 장애물에 의해 끝까지 갈 수 없는 경우.
    /// </summary>
    public bool IsPartiallyBlocked => ValidPath.Count > 0 && IsBlocked;

    /// <summary>
    /// 주어진 Mobility로 이 행동(이동 경로)을 감당할 수 있는지 확인합니다.
    /// </summary>
    public bool CanUnitAfford(int currentUnitMobility) => currentUnitMobility >= RequiredMobility;

    // --------------------------------------------------------------------------
    // 4. Constructor & Factory
    // --------------------------------------------------------------------------

    // private 생성자: 외부에서의 불변성 유지를 위해, 정적 팩토리를 강제합니다.
    private PathCalculationResult(
        IEnumerable<GridCoords> valid,
        IEnumerable<GridCoords> invalid,
        bool isBlocked,
        int requiredMobility)
    {
        // 입력받은 컬렉션을 ToList()로 복사하여, 외부 리스트가 변경되어도 이 객체는 영향을 받지 않도록 합니다.
        // 입력이 null이면 빈 리스트로 초기화합니다.
        ValidPath = (valid ?? Enumerable.Empty<GridCoords>()).ToList();
        InvalidPath = (invalid ?? Enumerable.Empty<GridCoords>()).ToList();

        IsBlocked = isBlocked;
        RequiredMobility = requiredMobility;
    }

    /// <summary>
    /// 새로운 경로 계산 결과 불변 객체를 생성합니다.
    /// </summary>
    /// <param name="valid">이동 가능한 경로 타일 리스트</param>
    /// <param name="invalid">이동 불가능한 경로 타일 리스트</param>
    /// <param name="isBlocked">장애물 존재 여부</param>
    /// <param name="requiredMobility">이 이동에 필요한 Mobility 값</param>
    public static PathCalculationResult Create(
        IEnumerable<GridCoords> valid,
        IEnumerable<GridCoords> invalid,
        bool isBlocked,
        int requiredMobility)
    {
        return new PathCalculationResult(valid, invalid, isBlocked, requiredMobility);
    }
}
