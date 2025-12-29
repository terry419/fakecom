using System;
using System.Collections.Generic;
using UnityEngine;

// [Factory] 상태 객체 생성, 캐싱, 의존성 주입 담당
public class SessionStateFactory
{
    private readonly SessionContext _context;
    private readonly Dictionary<SessionState, SessionStateBase> _stateCache = new Dictionary<SessionState, SessionStateBase>();

    public SessionStateFactory(SessionContext context)
    {
        _context = context;
    }

    public SessionStateBase GetOrCreate(SessionState stateEnum)
    {
        if (_stateCache.TryGetValue(stateEnum, out var state))
        {
            return state;
        }

        var newState = Create(stateEnum);
        if (newState == null)
        {
            // 지원되지 않는 상태에 대해 명시적인 예외를 발생시킴
            throw new NotSupportedException($"[Factory] 상태 '{stateEnum}'의 생성이 지원되지 않습니다.");
        }
        
        _stateCache[stateEnum] = newState;
        return newState;
    }

    private SessionStateBase Create(SessionState stateEnum)
    {
        switch (stateEnum)
        {
            case SessionState.Setup:
                return new SetupState(_context);

            case SessionState.TurnWaiting:
                return new TurnWaitingState(_context);
            
            case SessionState.BattleEnd:
                return new BattleEndState(_context);

            case SessionState.Error:
                return new ErrorState(_context);

            // case SessionState.PlayerTurn:
            //    return new PlayerTurnState(_context);

            default:
                // null을 반환하여 GetOrCreate에서 예외를 던지도록 함
                return null;
        }
    }
}
