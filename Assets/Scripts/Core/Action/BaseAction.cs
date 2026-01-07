using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public struct ActionExecutionResult
{
    public bool Success;
    public bool Cancelled;
    public string ErrorMessage;

    public static ActionExecutionResult Ok() => new ActionExecutionResult { Success = true };
    public static ActionExecutionResult Cancel() => new ActionExecutionResult { Cancelled = true };
    public static ActionExecutionResult Fail(string reason) => new ActionExecutionResult { Success = false, ErrorMessage = reason };
}

public enum ActionState { Disabled, Idle, Active, Running, Finished }

public abstract class BaseAction : MonoBehaviour
{
    protected Unit _unit;
    protected CancellationTokenSource _actionCts;

    public ActionState State { get; protected set; } = ActionState.Idle;

    public virtual void Initialize(Unit unit)
    {
        _unit = unit;
        State = ActionState.Idle;
    }

    public abstract string GetActionName();

    public virtual string GetBlockReason(GridCoords targetCoords = default)
    {
        if (_unit == null) return "No Unit";
        if (State == ActionState.Running) return "Already Running";
        return string.Empty;
    }

    public virtual bool CanExecute(GridCoords targetCoords = default)
    {
        return string.IsNullOrEmpty(GetBlockReason(targetCoords));
    }

    public virtual void OnSelect()
    {
        if (!CanExecute()) return;

        State = ActionState.Active;

        _actionCts?.Dispose();
        _actionCts = new CancellationTokenSource();
    }

    public virtual void OnExit()
    {
        State = ActionState.Idle;

        Cancel();
        _actionCts?.Dispose();
        _actionCts = null;
    }

    public void Cancel()
    {
        if (State == ActionState.Running && _actionCts != null)
        {
            _actionCts.Cancel();
        }
    }

    public abstract void OnUpdate(GridCoords mouseGrid);

    protected abstract UniTask<ActionExecutionResult> OnClickAsync(GridCoords mouseGrid, CancellationToken token);

    public async UniTask<ActionExecutionResult> ExecuteAsync(GridCoords mouseGrid)
    {
        if (!CanExecute(mouseGrid))
            return ActionExecutionResult.Fail(GetBlockReason(mouseGrid));

        State = ActionState.Running;

        if (_actionCts == null) _actionCts = new CancellationTokenSource();

        try
        {
            var result = await OnClickAsync(mouseGrid, _actionCts.Token);

            if (result.Success)
            {
                State = ActionState.Finished;
            }
            else if (result.Cancelled)
            {
                State = ActionState.Active;
            }
            else
            {
                State = ActionState.Active;
            }

            return result;
        }
        catch (System.OperationCanceledException)
        {
            State = ActionState.Active;
            return ActionExecutionResult.Cancel();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[{GetActionName()}] Execution Error: {ex.Message}");
            State = ActionState.Active;
            return ActionExecutionResult.Fail(ex.Message);
        }
    }
}
