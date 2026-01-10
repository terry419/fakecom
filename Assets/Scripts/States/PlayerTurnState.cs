using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerTurnState : BattleStateBase
{
    public override BattleState StateID => BattleState.PlayerTurn;
    private readonly BattleContext _context;

    public PlayerTurnState(BattleContext context)
    {
        _context = context;
    }

    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        // 1. 데이터 검증
        var activeUnitStatus = _context.Turn.ActiveUnit;
        if (activeUnitStatus == null || !activeUnitStatus.TryGetComponent<Unit>(out var unitComponent))
        {
            Debug.LogError("[PlayerTurnState] Invalid ActiveUnit. Reverting to Waiting.");
            // [Safety] 즉시 요청하면 BattleManager가 바쁠 수 있으므로 다음 프레임에 요청
            ChangeStateToWaitingAsync().Forget();
            return;
        }

        Debug.Log($"[PlayerTurnState] Start Turn: {activeUnitStatus.name}");

        // Deadlock 방지: Fire-and-Forget
        RunTurnLogic(unitComponent, cancellationToken).Forget();

        await UniTask.CompletedTask;
    }

    private async UniTaskVoid ChangeStateToWaitingAsync()
    {
        await UniTask.NextFrame();
        RequestTransition(BattleState.TurnWaiting);
    }

    private async UniTaskVoid RunTurnLogic(Unit unit, CancellationToken token)
    {
        var playerController = ServiceLocator.Get<PlayerController>();
        var inputManager = ServiceLocator.Get<InputManager>();

        if (playerController == null || inputManager == null)
        {
            Debug.LogError("[PlayerTurnState] Critical: Missing PlayerController or InputManager.");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        Action onTurnEndAction = () =>
        {
            Debug.Log("[PlayerTurnState] Turn End Requested via UI.");
            playerController.EndTurn();
        };

        // 성공적인 흐름 제어를 위한 플래그
        bool turnCompletedSuccessfully = false;

        try
        {
            // 1. 빙의 (Possess)
            bool isPossessSuccessful = await playerController.Possess(unit);
            if (!isPossessSuccessful)
            {
                Debug.LogError("[PlayerTurnState] Possess failed.");
                // 여기서 EndTurn을 부르면 안됨 (시작도 안했으므로)
                RequestTransition(BattleState.TurnWaiting, null);
                return;
            }

            // 2. 이벤트 구독
            inputManager.OnTurnEndInvoked += onTurnEndAction;

            // 3. 턴 로직 실행 (유저 입력 대기)
            await playerController.OnTurnStart();

            // 4. 로직 정상 종료 확인
            turnCompletedSuccessfully = true;
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[PlayerTurnState] Turn Cancelled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerTurnState] Error: {ex.Message}");
            RequestTransition(BattleState.Error, new ErrorPayload(ex));
        }
        finally
        {
            // 5. 정리 (Cleanup)
            if (inputManager != null)
                inputManager.OnTurnEndInvoked -= onTurnEndAction;

            // [Core Fix]
            // 상태 전환 요청 전에 *반드시* 턴을 물리적으로 종료시켜야 합니다.
            // 그래야 TurnWaitingState가 'ActiveUnit'을 null로 인식합니다.
            if (_context.Turn != null)
            {
                _context.Turn.EndTurn();
            }
        }

        // [Core Fix]
        // EndTurn()이 완전히 끝난 후에 전환 요청을 보냅니다.
        // (finally 블록 이후에 실행됨)
        if (turnCompletedSuccessfully)
        {
            Debug.Log("[PlayerTurnState] Controller logic finished. Requesting transition.");
            RequestTransition(BattleState.TurnWaiting);
        }
    }

    public override async UniTask Exit(CancellationToken cancellationToken)
    {
        if (ServiceLocator.TryGet<PlayerController>(out var playerController))
        {
            if (playerController.PossessedUnit != null)
            {
                await playerController.Unpossess();
            }
        }

        Debug.Log("[PlayerTurnState] Exit Complete.");
        await UniTask.CompletedTask;
    }
}