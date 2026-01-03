using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [State] 치명적 오류 처리 상태
public class ErrorState : BattleStateBase
{
    public override BattleState StateID => BattleState.Error;

    public ErrorState(BattleContext context) { } // Context는 필요 없을 수도 있지만 일관성을 위해 받음

    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        // 전환 시 ErrorPayload를 받아서 로그를 출력
        if (payload is ErrorPayload errorPayload)
        {
            Debug.LogError($"<color=red>[ErrorState] 치명적인 오류가 감지되었습니다. 게임을 중단합니다.\n--- {errorPayload.Exception.GetType().Name} ---\n{errorPayload.Exception.Message}\n{errorPayload.Exception.StackTrace}</color>");
        }
        else
        {
            Debug.LogError("<color=red>[ErrorState] 알 수 없는 치명적인 오류입니다. 게임을 중단합니다.</color>");
        }

        // 실제 게임에서는 에러 UI를 띄우고 재시작/종료를 안내합니다.
        // 여기서는 더 이상 진행되지 않도록 무한정 대기합니다.
        await UniTask.WaitUntilCanceled(cancellationToken);
    }

    public override void Update()
    {
        // 이 상태에서는 아무것도 업데이트하지 않아 게임을 멈춥니다.
    }
}
