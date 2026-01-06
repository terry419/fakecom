using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [Refactored] PlayerController에게 턴 제어권 위임 및 입력 활성화 보장
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
        Debug.Log($"<color=magenta>[TRACER] 1. PlayerTurnState 진입 성공! (ActiveUnit: {_context.Turn.ActiveUnit?.name})</color>");

        // 1. 데이터 검증
        var activeUnitStatus = _context.Turn.ActiveUnit;
        if (activeUnitStatus == null)
        {
            Debug.LogError("[PlayerTurnState] ActiveUnit is null. Reverting to Waiting.");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

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

        // 3. 이벤트 연결 (UI 턴 종료 버튼 -> PlayerController 종료)
        // 로컬 함수나 델리게이트를 사용하여 구독 해제를 보장합니다.
        Action onTurnEndAction = () => playerController.EndTurn();
        inputManager.OnTurnEndInvoked += onTurnEndAction;

        try
        {
            // 4. Unit 빙의 (Possess)
            bool isPossessSuccessful = await playerController.Possess(unitComponent);
            if (!isPossessSuccessful)
            {
                Debug.LogError("[PlayerTurnState] Failed to Possess unit.");
                RequestTransition(BattleState.TurnWaiting, null);
                return;
            }

            // 5. [Fix] PlayerController의 턴 로직 실행 (입력 활성화 포함)
            // OnTurnStart 내부에서 _inputHandler.SetActive(true)가 호출됩니다.
            // 플레이어가 턴을 종료할 때까지 여기서 대기합니다.
            await playerController.OnTurnStart();

            // 6. 턴 로직이 끝나면 상태 전환 요청
            Debug.Log("[PlayerTurnState] Controller logic finished. Requesting transition.");
            RequestTransition(BattleState.TurnWaiting);
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
            // 7. 이벤트 구독 해제
            if (inputManager != null)
            {
                inputManager.OnTurnEndInvoked -= onTurnEndAction;
            }
        }
    }

    public override async UniTask Exit(CancellationToken cancellationToken)
    {
        var playerController = ServiceLocator.Get<PlayerController>();

        // 1. 빙의 해제 (Unpossess)
        if (playerController != null)
        {
            await playerController.Unpossess();
        }

        // 2. TurnManager에 턴 종료 통지 (AP 리셋, 턴 카운트 증가 등 처리)
        if (_context.Turn != null)
        {
            _context.Turn.EndTurn();
        }

        Debug.Log("[PlayerTurnState] Turn Ended & Cleaned up.");
    }

    public override void Update() { }
}