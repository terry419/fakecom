using System;
using System.Threading;
using Cysharp.Threading.Tasks;

// 모든 상태의 부모 클래스
public abstract class SessionStateBase
{
    // 상태가 매니저에게 (다음 상태, 데이터)를 담아 전환을 요청하는 이벤트
    public event Action<SessionState, StatePayload> OnRequestTransition;

    // 현재 상태의 Enum ID
    public abstract SessionState StateID { get; }

    public virtual async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        await UniTask.CompletedTask;
    }

    public virtual async UniTask Exit(CancellationToken cancellationToken)
    {
        await UniTask.CompletedTask;
    }

    public virtual void Update() { }

    protected void RequestTransition(SessionState nextState, StatePayload payload = null)
    {
        OnRequestTransition?.Invoke(nextState, payload);
    }
}
