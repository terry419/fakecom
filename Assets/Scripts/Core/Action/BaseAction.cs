using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

// [New] 결과에 따른 후속 조치
public enum ActionConsequence
{
    None = 0,
    EndTurn,
    SwitchToDefaultAction,
    WaitForInput
}

public struct ActionExecutionResult
{
    public bool Success;
    public bool Cancelled;
    public string ErrorMessage;

    public ActionConsequence Consequence;

    public static ActionExecutionResult Ok(ActionConsequence consequence = ActionConsequence.None)
        => new ActionExecutionResult { Success = true, Consequence = consequence };

    public static ActionExecutionResult Cancel()
        => new ActionExecutionResult { Cancelled = true, Consequence = ActionConsequence.None };

    public static ActionExecutionResult Fail(string reason)
        => new ActionExecutionResult { Success = false, ErrorMessage = reason, Consequence = ActionConsequence.None };
}

public enum ActionState { Disabled, Idle, Active, Running, Finished }

public abstract class BaseAction : MonoBehaviour
{
    protected Unit _unit;
    protected CancellationTokenSource _actionCts;

    public ActionState State { get; protected set; } = ActionState.Idle;

    public virtual void Initialize(Unit unit)
    {
        // [Fix] OnExit() 호출 제거! (자식 클래스 변수가 초기화되기 전에 호출되어 NullRef 유발함)

        // 대신 안전하게 CTS만 정리
        if (_actionCts != null)
        {
            _actionCts.Cancel();
            _actionCts.Dispose();
            _actionCts = null;
        }

        // 실행 중이었다면 상태 강제 초기화 경고
        if (State == ActionState.Running)
        {
            Debug.LogWarning($"[{GetActionName()}] Initializing while running. State reset forced.");
        }

        _unit = unit;
        State = ActionState.Idle;
    }

    public abstract string GetActionName();

    public virtual string GetBlockReason(GridCoords targetCoords = default)
    {
        if (_unit == null) return "No Unit";
        if (State == ActionState.Running) return "Already Running";
        if (State == ActionState.Finished) return "Action Finished";
        return string.Empty;
    }

    public virtual bool CanExecute(GridCoords targetCoords = default)
    {
        return string.IsNullOrEmpty(GetBlockReason(targetCoords));
    }

    public virtual void OnSelect()
    {
        string reason = GetBlockReason();
        if (!string.IsNullOrEmpty(reason))
        {
            Debug.LogWarning($"[{GetActionName()}] Cannot Select: {reason}");
            return;
        }

        State = ActionState.Active;
    }

    public virtual void OnExit()
    {
        if (State != ActionState.Disabled)
        {
            State = ActionState.Idle;
        }
        DisposeToken();
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
        Debug.Log($"[{GetActionName()}] ExecuteAsync Started at {mouseGrid}");

        string reason = GetBlockReason(mouseGrid);
        if (!string.IsNullOrEmpty(reason))
        {
            Debug.LogWarning($"[{GetActionName()}] Execution Blocked: {reason}");
            return ActionExecutionResult.Fail(reason);
        }

        State = ActionState.Running;
        ResetToken();

        try
        {
            var result = await OnClickAsync(mouseGrid, _actionCts.Token);

            if (result.Success)
            {
                Debug.Log($"[{GetActionName()}] Execution Success");
                State = ActionState.Finished;
            }
            else
            {
                if (result.Cancelled)
                    Debug.Log($"[{GetActionName()}] Execution Canceled (Result)");
                else
                    Debug.LogWarning($"[{GetActionName()}] Execution Failed: {result.ErrorMessage}");

                State = ActionState.Active;
            }

            return result;
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log($"[{GetActionName()}] Execution Canceled (Exception)");
            State = ActionState.Active;
            return ActionExecutionResult.Cancel();
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            State = ActionState.Active;
            return ActionExecutionResult.Fail($"{ex.GetType().Name}: {ex.Message}");
        }
    }

    protected void ResetToken()
    {
        DisposeToken();
        _actionCts = new CancellationTokenSource();
    }

    private void DisposeToken()
    {
        if (_actionCts != null)
        {
            _actionCts.Cancel();
            _actionCts.Dispose();
            _actionCts = null;
        }
    }
}