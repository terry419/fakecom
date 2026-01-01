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

        // [개선점 3] 전체 예외 처리 및 에러 상태 전환
        try
        {
            // 1. ServiceLocator 안전하게 가져오기
            TilemapGenerator generator = null;
            try
            {
                generator = ServiceLocator.Get<TilemapGenerator>();
            }
            catch (Exception)
            {
                // 여기서 잡아서 아래로 넘김
                throw new Exception("ServiceLocator failed to find TilemapGenerator.");
            }

            // 2. 맵 생성 실행
            if (generator != null)
            {
                await generator.GenerateAsync();
            }
            else
            {
                throw new Exception("TilemapGenerator is null.");
            }

            Debug.Log("[SetupState] Map Ready. Waiting for confirmation...");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SetupState] Map generation failed: {ex.Message}");

            // 부모 클래스의 protected 메서드 사용 (이벤트 호출)
            RequestTransition(SessionState.Error, new ErrorPayload(ex));
            return;
        }

        // 3. 플레이어 확인 대기 (Enter는 끝나고 백그라운드에서 실행)
        // 토큰 연결: 상태가 종료되면(Exit) 대기도 취소되도록 함
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