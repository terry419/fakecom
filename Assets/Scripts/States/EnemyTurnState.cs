using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class EnemyTurnState : BattleStateBase
{
    public override BattleState StateID => BattleState.EnemyTurn;

    private readonly BattleContext _context;

    public EnemyTurnState(BattleContext context)
    {
        _context = context;
    }

    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        var activeUnitStatus = _context.Turn.ActiveUnit;

        // 1. 유효성 검사
        if (activeUnitStatus == null || !activeUnitStatus.TryGetComponent<Unit>(out var unit))
        {
            Debug.LogError("[EnemyTurnState] 유닛이 없습니다. 턴을 종료합니다.");
            _context.Turn.EndTurn();
            // 비동기로 전환 요청 (Enter가 끝난 뒤 실행되도록)
            ChangeStateToWaitingAsync().Forget();
            return;
        }

        Debug.Log($"<color=red>[EnemyTurnState] 적군 턴 시작: {unit.name}</color>");

        // 2. [핵심 수정] AI 로직을 Fire-and-Forget으로 실행
        // 이렇게 해야 Enter()가 즉시 반환되어 BattleManager의 _isTransitioning 잠금이 풀립니다.
        RunEnemyLogic(unit, cancellationToken).Forget();

        await UniTask.CompletedTask;
    }

    private async UniTaskVoid RunEnemyLogic(Unit unit, CancellationToken token)
    {
        try
        {
            // Unit.OnTurnStart() -> EnemyUnitController.OnTurnStart() -> ThinkingProcess()
            // 여기서 1초 대기함
            await unit.OnTurnStart();
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[EnemyTurnState] AI Canceled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[EnemyTurnState] AI Error: {ex.Message}");
        }

        // 3. 로직이 다 끝난 뒤에 전환 요청
        // 이제 Enter가 이미 끝났으므로 _isTransitioning이 false 상태라 전환이 승인됨.
        Debug.Log("[EnemyTurnState] AI 종료. 대기 상태로 전환 요청.");
        RequestTransition(BattleState.TurnWaiting);
    }

    private async UniTaskVoid ChangeStateToWaitingAsync()
    {
        await UniTask.NextFrame();
        RequestTransition(BattleState.TurnWaiting);
    }

    public override async UniTask Exit(CancellationToken cancellationToken)
    {
        Debug.Log("[EnemyTurnState] Exit Complete.");
        await UniTask.CompletedTask;
    }
}