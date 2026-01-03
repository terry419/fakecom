using System;

// 모든 매니저 의존성을 담는 컨테이너 클래스
public class BattleContext
{
    public readonly MapManager Map;
    public readonly TurnManager Turn;
    public readonly IUIManager UI;
    // public readonly UnitManager Unit;

    public BattleContext(MapManager map, TurnManager turn, IUIManager ui)
    {
        // Fail-Fast: 필수 의존성이 null이면 즉시 에러를 발생시킵니다.
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Turn = turn ?? throw new ArgumentNullException(nameof(turn));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
    }
}
