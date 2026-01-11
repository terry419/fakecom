using System;
using System.Collections.Generic;
using UnityEngine;

// [Factory] 상태 객체 생성, 캐싱, 의존성 주입 담당
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
        {
            return state;
        }

        var newState = Create(stateEnum);
        if (newState == null)
        {
            throw new NotSupportedException($"[Factory] 상태 '{stateEnum}'의 생성이 지원되지 않습니다.");
        }

        _stateCache[stateEnum] = newState;
        return newState;
    }

    private BattleStateBase Create(BattleState stateEnum)
    {
        switch (stateEnum)
        {
            case BattleState.Setup:
                return new SetupState(_context);

            case BattleState.TurnWaiting:
                return new TurnWaitingState(_context);

            case BattleState.PlayerTurn:
                return new PlayerTurnState(_context);

            // [Step 4 Fix] 주석 해제! 적 턴 상태 생성
            case BattleState.EnemyTurn:
                return new EnemyTurnState(_context);

            case BattleState.BattleEnd:
                return new BattleEndState(_context);

            case BattleState.Error:
                return new ErrorState(_context);

            default:
                return null;
        }
    }
}