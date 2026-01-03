using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public abstract class BattleStateBase
{
    public event Action<BattleState, StatePayload> OnRequestTransition;
    public abstract BattleState StateID { get; }

    public virtual async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        await UniTask.CompletedTask;
    }

    public virtual async UniTask Exit(CancellationToken cancellationToken)
    {
        await UniTask.CompletedTask;
    }

    public virtual void Update() { }

    protected void RequestTransition(BattleState nextState, StatePayload payload = null)
    {
        OnRequestTransition?.Invoke(nextState, payload);
    }
}