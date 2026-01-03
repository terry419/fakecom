using System;
using System.Collections.Generic;

public class BattleStateFactory
{
    private readonly BattleContext _context;
    private readonly Dictionary<BattleState, BattleStateBase> _stateCache = new Dictionary<BattleState, BattleStateBase>();

    public BattleStateFactory(BattleContext context)
    {
        _context = context;
    }

    public BattleStateBase GetOrCreate(BattleState stateEnum)
    {
        if (_stateCache.TryGetValue(stateEnum, out var state))
            return state;

        var newState = Create(stateEnum);
        if (newState != null) _stateCache[stateEnum] = newState;

        return newState;
    }

    private BattleStateBase Create(BattleState stateEnum)
    {
        switch (stateEnum)
        {
            case BattleState.Setup: return new SetupState(_context);
            case BattleState.TurnWaiting: return new TurnWaitingState(_context);
            case BattleState.BattleEnd: return new BattleEndState(_context);
            case BattleState.Error: return new ErrorState(_context); // ErrorState 생성자 수정 필요
                                                                     // case BattleState.PlayerTurn: return new PlayerTurnState(_context);

            default: throw new NotSupportedException($"[BattleFactory] 지원하지 않는 상태: {stateEnum}");
        }
    }
}