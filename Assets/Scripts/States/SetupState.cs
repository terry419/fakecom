using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// [State] 맵/유닛 배치 및 전투 시작 대기
public class SetupState : SessionStateBase
{
    // [Fix 1] 부모의 abstract 프로퍼티 구현
    public override SessionState StateID => SessionState.Setup;

    // 생성자를 통해 SessionContext 주입 (SessionStateFactory 구조 유지)
    private readonly SessionContext _context;
    private CancellationTokenSource _linkedCts;

    public SetupState(SessionContext context)
    {
        _context = context;
    }

    // [Fix 2] 부모 클래스와 서명 일치 (StatePayload payload, CancellationToken cancellationToken)
    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        Debug.Log("[SetupState] Entering Setup State...");

        try
        {
            // [수정] TilemapGenerator를 직접 생성하여 사용
            Debug.Log("[SetupState] Generating visual map...");
            var generator = new TilemapGenerator();
            await generator.GenerateAsync();

            Debug.Log("[SetupState] Map Ready. Waiting for confirmation...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SetupState] Map generation failed: {ex.Message}\n{ex.StackTrace}");
            RequestTransition(SessionState.Error, new ErrorPayload(ex));
            return;
        }

        // 플레이어 확인 대기
        _linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        WaitForPlayerConfirmation(_linkedCts.Token).Forget();
    }

    private async UniTaskVoid WaitForPlayerConfirmation(CancellationToken token)
    {
        try
        {
            // Context에 이미 존재하는 UI 매니저 사용 (Mock 객체 새로 생성 X)
            var ui = _context.UI;

            var confirmSource = new UniTaskCompletionSource<bool>();
            Action onClick = () => confirmSource.TrySetResult(true);

            try
            {
                ui.OnStartButtonClick += onClick;
                ui.ShowStartButton();

                // (테스트 편의용) 1.5초 후 자동 시작 시뮬레이션
                // 필요 없다면 이 부분은 주석 처리 가능
                UniTask.Delay(1500, cancellationToken: token)
                       .ContinueWith(() => onClick())
                       .Forget();

                // UI 클릭 혹은 취소 토큰 발동 대기
                using (token.Register(() => confirmSource.TrySetCanceled()))
                {
                    await confirmSource.Task;
                }
            }
            finally
            {
                // 정리
                ui.HideStartButton();
                ui.OnStartButtonClick -= onClick;
            }

            Debug.Log("[SetupState] Player Confirmed. Transitioning to TurnWaiting...");
            RequestTransition(SessionState.TurnWaiting);
        }
        catch (OperationCanceledException)
        {
            Debug.Log("[SetupState] Confirmation cancelled.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SetupState] UI Error: {ex.Message}");
            RequestTransition(SessionState.Error, new ErrorPayload(ex));
        }
    }

    // [Fix 2] 부모 클래스와 서명 일치
    public override async UniTask Exit(CancellationToken cancellationToken)
    {
        // 내부 비동기 작업 정리
        if (_linkedCts != null)
        {
            _linkedCts.Cancel();
            _linkedCts.Dispose();
            _linkedCts = null;
        }
        await UniTask.CompletedTask;
    }
}
// [Fix 3] ErrorPayload 클래스 중복 정의 삭제 (ErrorPayload.cs 사용)