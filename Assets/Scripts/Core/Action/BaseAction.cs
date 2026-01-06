using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

// [변경] AP Cost는 이제 의미가 없으므로 0을 기본으로 하는 더미 구조체 혹은 삭제 가능.
// 호환성을 위해 남겨두되 로직에서 무시합니다.
[System.Serializable]
public struct ActionCost
{
    public int AP;
}

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

    // [변경] AP 비용 계산 메서드는 남겨두지만 0을 반환 (인터페이스 유지)
    public virtual ActionCost GetActionCost() => new ActionCost { AP = 0 };

    public virtual string GetBlockReason(GridCoords targetCoords = default)
    {
        if (_unit == null) return "No Unit";
        if (State == ActionState.Running) return "Already Running";
        // [수정] AP 부족 체크 로직 삭제됨
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
                // [수정] ApplyCost(비용 차감) 로직 삭제됨
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