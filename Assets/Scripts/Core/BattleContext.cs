using System;

// [Refactor] 객체 이니셜라이저 패턴 및 유효성 검사 지원
public class BattleContext
{
    public BattleManager BattleManager { get; set; }
    public MapManager Map { get; set; }
    public TurnManager Turn { get; set; }
    public IUIManager UI { get; set; }

    // [Fix 3] 필수 의존성 검증 메서드
    public void Validate()
    {
        if (BattleManager == null) throw new InvalidOperationException("[BattleContext] BattleManager is null");
        if (Map == null) throw new InvalidOperationException("[BattleContext] MapManager is null");
        if (Turn == null) throw new InvalidOperationException("[BattleContext] TurnManager is null");
        if (UI == null) throw new InvalidOperationException("[BattleContext] IUIManager is null");
    }
}