using System;

/// <summary>
/// [GDD 1.4, 5.6] 타일 위에 배치될 수 있는 객체의 공통 인터페이스.
/// </summary>
public interface ITileOccupant
{
    // --------------------------------------------------------------------------
    // 1. 식별 및 속성
    // --------------------------------------------------------------------------

    /// <summary>
    /// 객체의 타입 (Unit, Item 등). 캐스팅 비용을 줄이기 위해 사용.
    /// </summary>
    OccupantType Type { get; }

    /// <summary>
    /// 이동을 막는 객체인가? (Unit=True, Item=False)
    /// </summary>
    bool IsBlockingMovement { get; }

    /// <summary>
    /// 엄폐를 제공하는가?
    /// </summary>
    bool IsCover { get; }


    // --------------------------------------------------------------------------
    // 2. 상태 변화 알림 (Tile 캐시 갱신용)
    // --------------------------------------------------------------------------

    /// <summary>
    /// 이동 방해 여부가 변경되었을 때 (예: 유닛 사망, 은신)
    /// </summary>
    event Action<bool> OnBlockingChanged;

    /// <summary>
    /// 엄폐 제공 여부가 변경되었을 때 (예: 장애물 파괴)
    /// </summary>
    event Action<bool> OnCoverChanged;


    // --------------------------------------------------------------------------
    // 3. 생명주기 (Tile에 의해 호출됨)
    // --------------------------------------------------------------------------

    /// <summary>
    /// 타일에 성공적으로 배치된 후 호출됨.
    /// </summary>
    void OnAddedToTile(Tile tile);

    /// <summary>
    /// 타일에서 제거된 후 호출됨.
    /// </summary>
    void OnRemovedFromTile(Tile tile);
}