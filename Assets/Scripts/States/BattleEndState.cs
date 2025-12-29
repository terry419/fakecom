using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [State] 전투 종료 상태
public class BattleEndState : SessionStateBase
{
    public override SessionState StateID => SessionState.BattleEnd;
    private readonly SessionContext _context;

    public BattleEndState(SessionContext context)
    {
        _context = context;
    }

    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        Debug.Log("<color=green>[BattleEndState] 전투 종료. 결과 화면으로 전환합니다.</color>");
        await UniTask.Delay(1500, cancellationToken: cancellationToken);
        
        // 결과창(Resolution) 상태로 전환 요청
        RequestTransition(SessionState.Resolution);
    }
}
