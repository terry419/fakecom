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

            // [복구 완료] 이제 PlayerTurn 요청이 오면 정상적으로 상태를 생성합니다.
            case BattleState.PlayerTurn:
                return new PlayerTurnState(_context);

            // (참고) 적 턴(EnemyTurnState) 스크립트도 만드셨다면 아래 주석을 푸세요.
            // case BattleState.EnemyTurn:
            //    return new EnemyTurnState(_context);

            case BattleState.BattleEnd:
                return new BattleEndState(_context);

            case BattleState.Error:
                return new ErrorState(_context);

            default:
                return null;
        }
    }
}