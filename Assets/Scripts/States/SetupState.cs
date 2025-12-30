using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [State] 맵/유닛 배치 및 전투 시작 대기
public class SetupState : SessionStateBase
{
    public override SessionState StateID => SessionState.Setup;
    private readonly SessionContext _context;
    private UniTaskCompletionSource<bool> _startConfirmSource;

    public SetupState(SessionContext context)
    {
        _context = context;
    }

    public override UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        Debug.Log("[SetupState] 맵 로딩 및 유닛 배치...");
        
        // Enter 메서드는 즉시 완료되어야 하므로, 대기 및 전환 로직은
        // 별도의 비동기 메서드에서 처리하도록 분리합니다.
        WaitForConfirmationAndTransition(cancellationToken).Forget();

        return UniTask.CompletedTask;
    }

    private async UniTaskVoid WaitForConfirmationAndTransition(CancellationToken cancellationToken)
    {
        await UniTask.Delay(500, cancellationToken: cancellationToken);

        Debug.Log("[SetupState] <color=yellow>배치 완료. 전투 시작 대기 중...</color>");

        await WaitForPlayerConfirmation(cancellationToken);

        RequestTransition(SessionState.TurnWaiting);
    }
    
    // 외부(UI)에서 이 메서드를 호출하여 대기를 해제
    public void NotifyStartConfirmation()
    {
        _startConfirmSource?.TrySetResult(true);
    }

    private async UniTask WaitForPlayerConfirmation(CancellationToken token)
    {
        _startConfirmSource = new UniTaskCompletionSource<bool>();
        
        // try-finally로 이벤트 구독/해제를 보장하고 IUIManager 사용
        try
        {
            _context.UI.OnStartButtonClick += NotifyStartConfirmation;
            _context.UI.ShowStartButton();
            
            // 취소 토큰 발동 시 대기도 함께 취소
            using (token.Register(() => _startConfirmSource.TrySetCanceled()))
            {
                // UI에서 버튼 클릭 이벤트를 받을 때까지 대기
                await _startConfirmSource.Task;
            }
        }
        finally
        {
            _context.UI.HideStartButton();
            _context.UI.OnStartButtonClick -= NotifyStartConfirmation;
        }
    }
}
