using System;

// 전투 FSM 상태들이 공유하는 의존성 컨테이너
public class BattleContext
{
    public readonly MapManager Map;
    public readonly TurnManager Turn;
    public readonly IUIManager UI;

    public BattleContext(MapManager map, TurnManager turn, IUIManager ui)
    {
        Map = map ?? throw new ArgumentNullException(nameof(map));
        Turn = turn ?? throw new ArgumentNullException(nameof(turn));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
    }
}