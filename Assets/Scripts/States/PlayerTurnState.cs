using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerTurnState : BattleStateBase
{
    public override BattleState StateID => BattleState.PlayerTurn;
    private readonly BattleContext _context;

    // [개선 3] 메모리 누수 방지를 위해 관리되는 소스
    private UniTaskCompletionSource _turnEndSource;

    public PlayerTurnState(BattleContext context)
    {
        _context = context;
    }

    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        Debug.Log($"<color=magenta>[TRACER] 1. PlayerTurnState 진입 성공! (ActiveUnit: {_context.Turn.ActiveUnit?.name})</color>");
        // 1. 데이터 검증 및 GetComponent 최적화
        var activeUnitStatus = _context.Turn.ActiveUnit;
        if (activeUnitStatus == null)
        {
            Debug.LogError("[PlayerTurnState] ActiveUnit is null. Reverting to Waiting.");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        // [개선 4] GetComponent 호출 최적화 (ActiveUnit이 변경될 때만 호출됨)
        // UnitStatus와 Unit 컴포넌트가 같은 오브젝트에 있다고 가정
        if (!activeUnitStatus.TryGetComponent<Unit>(out var unitComponent))
        {
            Debug.LogError($"[PlayerTurnState] '{activeUnitStatus.name}' has no Unit component!");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        Debug.Log($"[PlayerTurnState] Start Turn: {activeUnitStatus.name}");

        // 2. 매니저 참조
        var playerController = ServiceLocator.Get<PlayerController>();
        var inputManager = ServiceLocator.Get<InputManager>();

        if (playerController == null || inputManager == null)
        {
            Debug.LogError("[PlayerTurnState] Missing Managers.");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        // 3. [개선 1] Race Condition 방지: await 전에 이벤트 리스너 설정
        _turnEndSource = new UniTaskCompletionSource();
        inputManager.OnTurnEndInvoked += OnTurnEndSignal;

        try
        {
            // 4. [개선 1 & 5] Possess 성공 여부 확인 및 bool 반환 처리
            // (PlayerController.Possess가 UniTask<bool>을 반환하도록 수정되어야 함)
            bool isPossessSuccessful = await playerController.Possess(unitComponent);

            if (!isPossessSuccessful)
            {
                Debug.LogError("[PlayerTurnState] Failed to Possess unit.");
                RequestTransition(BattleState.TurnWaiting, null);
                return;
            }

            // 5. 턴 종료 대기 (CancellationToken 연동)
            await _turnEndSource.Task.AttachExternalCancellation(cancellationToken);
        }
        catch (System.OperationCanceledException)
        {
            // 상태 전환이나 게임 종료로 인한 취소는 정상 흐름
        }
        finally
        {
            // [개선 1] 이벤트 구독 해제 보장
            if (inputManager != null)
            {
                inputManager.OnTurnEndInvoked -= OnTurnEndSignal;
            }
        }
    }

    private void OnTurnEndSignal()
    {
        // 이미 완료된 상태가 아니라면 결과 설정
        if (_turnEndSource != null && !_turnEndSource.Task.Status.IsCompleted())
        {
            _turnEndSource.TrySetResult();
        }
    }

    public override async UniTask Exit(CancellationToken cancellationToken)
    {
        // [개선 3] CompletionSource 명시적 정리 (메모리 누수 방지)
        if (_turnEndSource != null)
        {
            _turnEndSource.TrySetCanceled(); // 남은 Task가 있다면 취소 처리
            _turnEndSource = null;
        }

        var playerController = ServiceLocator.Get<PlayerController>();

        // [개선 2] Unpossess 완료 대기 (정리 보장)
        if (playerController != null)
        {
            await playerController.Unpossess();
        }

        // 턴 종료 처리
        if (_context.Turn != null)
        {
            _context.Turn.EndTurn();
        }

        Debug.Log("[PlayerTurnState] Turn Ended. Cleaning up.");

        // 다음 상태로 전환
        RequestTransition(BattleState.TurnWaiting, null);
    }

    public override void Update() { }
}