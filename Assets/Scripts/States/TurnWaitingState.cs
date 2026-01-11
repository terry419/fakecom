using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TurnWaitingState : BattleStateBase
{
    public override BattleState StateID => BattleState.TurnWaiting;
    private readonly BattleContext _context;

    public TurnWaitingState(BattleContext context)
    {
        _context = context;
    }

    public override UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        // [TRACER] 진입 확인
        Debug.Log("<color=magenta>[TRACER] TurnWaitingState.Enter 진입. 이벤트 구독 및 즉시 리턴.</color>");

        // 1. 이벤트 구독
        _context.Turn.OnTurnStarted += HandleTurnStarted;

        // 2. 만약 이미 ActiveUnit이 있다면? (타이밍 이슈 방지)
        if (_context.Turn.ActiveUnit != null)
        {
            // [중요] 여기서 바로 RequestTransition을 하면 BattleManager가 '전환 중'이라 무시합니다.
            // 따라서 Enter가 완전히 종료된 후(NextFrame)에 실행되도록 예약합니다.
            ChangeStateAsync(_context.Turn.ActiveUnit).Forget();
        }

        // 3. [핵심] 여기서 기다리지 않고 즉시 BattleManager에게 제어권을 돌려줍니다.
        // 그래야 BattleManager가 "TurnWaiting으로 전환 완료" 상태가 됩니다.
        return UniTask.CompletedTask;
    }

    private void HandleTurnStarted(UnitStatus turnUnit)
    {
        // 이벤트가 발생했을 때도 비동기로 처리하여 안전하게 전환합니다.
        ChangeStateAsync(turnUnit).Forget();
    }

    // 상태 전환을 안전하게 처리하는 비동기 메서드
    private async UniTaskVoid ChangeStateAsync(UnitStatus unitStatus)
    {
        // BattleManager가 현재 상태 처리를 끝낼 틈을 줍니다.
        await UniTask.NextFrame();

        if (unitStatus == null || !unitStatus.TryGetComponent<Unit>(out var unit))
        {
            Debug.LogError("[TurnWaitingState] 유효하지 않은 유닛입니다. 턴을 스킵합니다.");
            _context.Turn.EndTurn();
            return;
        }

        Debug.Log($"<color=magenta>[TRACER] 턴 유닛 감지: {unit.name} ({unit.Faction}). 상태 전환 요청.</color>");

        // [핵심] 팩션에 따른 상태 분기
        if (unit.Faction == Faction.Player)
        {
            RequestTransition(BattleState.PlayerTurn, null);
        }
        else if (unit.Faction == Faction.Enemy)
        {
            RequestTransition(BattleState.EnemyTurn, null); // EnemyTurnState로 전환
        }
        else
        {
            // 중립 유닛 등 기타 처리 (필요 시 구현)
            Debug.LogWarning($"[TurnWaitingState] 알 수 없는 팩션: {unit.Faction}. 턴을 넘깁니다.");
            _context.Turn.EndTurn();
        }
    }

    public override UniTask Exit(CancellationToken cancellationToken)
    {
        // 이벤트 구독 해제
        if (_context.Turn != null)
        {
            _context.Turn.OnTurnStarted -= HandleTurnStarted;
        }
        return UniTask.CompletedTask;
    }

    public override void Update() { }
}