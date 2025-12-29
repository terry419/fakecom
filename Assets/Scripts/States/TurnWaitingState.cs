using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [State] 다음 턴 계산 대기
public class TurnWaitingState : SessionStateBase
{
    public override SessionState StateID => SessionState.TurnWaiting;
    private readonly SessionContext _context;

    public TurnWaitingState(SessionContext context)
    {
        _context = context;
    }

    public override UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        // 실제 로직은 별도의 비동기 메서드로 분리하고, Enter는 즉시 종료합니다.
        CalculateAndTransition(cancellationToken).Forget();
        return UniTask.CompletedTask;
    }

    private async UniTaskVoid CalculateAndTransition(CancellationToken cancellationToken)
    {
        Debug.Log("[TurnWaitingState] 다음 턴 계산 중...");

        // Unit nextUnit = await _context.Turn.CalculateNextTurnAsync(cancellationToken); // 실제 턴 계산 로직
        await UniTask.Delay(300, cancellationToken: cancellationToken);

        Debug.Log("[TurnWaitingState] 턴 계산 완료. 플레이어 턴으로 전환합니다.");

        // 다음 상태(PlayerTurn)로 명시적 전환 요청
        // var nextPayload = new PlayerTurnPayload { TurnUnit = nextUnit }; // 필요한 경우 Payload와 함께
        RequestTransition(SessionState.PlayerTurn); // 임시로 Payload 없이 PlayerTurn으로 전환
    }
}
