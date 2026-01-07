using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class MoveAction : BaseAction
{
    private MovementPlanner _planner;
    private MapManager _mapManager;

    public event Action<HashSet<GridCoords>, GridCoords> OnShowReachable;
    public event Action<List<GridCoords>, List<GridCoords>> OnShowPath;
    public event Action OnClearVisuals;

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        _mapManager = ServiceLocator.Get<MapManager>();
        _planner = new MovementPlanner(_mapManager);
    }

    public override string GetActionName() => "Move";

    // ... (CanExecute, GetBlockReason 생략, 기존과 동일) ...
    public override bool CanExecute(GridCoords targetCoords = default)
    {
        if (State == ActionState.Disabled || State == ActionState.Running) return false;
        if (_unit.CurrentMobility <= 0) return false;
        if (targetCoords != default)
        {
            var result = _planner.CalculatePath(_unit, targetCoords);
            if (!result.IsValidMovePath) return false;
        }
        return true;
    }
    public override string GetBlockReason(GridCoords targetCoords = default) => base.GetBlockReason(targetCoords);


    private void RefreshMoveVisuals()
    {
        Debug.Log("[MoveAction-Debug] RefreshMoveVisuals Called.");

        _planner.CalculateReachableArea(_unit);

        // [LOG 2] 이벤트 구독자 확인
        if (OnShowReachable == null)
        {
            Debug.LogError("[MoveAction-Debug] OnShowReachable Event has NO SUBSCRIBERS! (Visualizer missing?)");
        }
        else
        {
            Debug.Log($"[MoveAction-Debug] Invoking OnShowReachable. Subscriber Count: {OnShowReachable.GetInvocationList().Length}");
            OnShowReachable.Invoke(_planner.CachedReachableTiles, _unit.Coordinate);
        }
    }

    public override void OnSelect()
    {
        base.OnSelect();
        Debug.Log($"[MoveAction-Debug] OnSelect Called. State: {State}");

        if (State == ActionState.Active)
        {
            RefreshMoveVisuals();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        OnClearVisuals?.Invoke();
        _planner.InvalidatePathCache();
    }

    public override void OnUpdate(GridCoords mouseGrid)
    {
        if (State != ActionState.Active) return;

        PathCalculationResult result = _planner.CalculatePath(_unit, mouseGrid);

        if (result.HasAnyPath)
            OnShowPath?.Invoke(result.ValidPath.ToList(), result.InvalidPath.ToList());
        else
            OnShowPath?.Invoke(null, null);
    }

    protected override async UniTask<ActionExecutionResult> OnClickAsync(GridCoords mouseGrid, CancellationToken token)
    {
        PathCalculationResult result = _planner.CalculatePath(_unit, mouseGrid);

        if (!result.IsValidMovePath) return ActionExecutionResult.Fail("Invalid Path");
        if (result.ValidPath.Count == 0 || !result.ValidPath.Last().Equals(mouseGrid)) return ActionExecutionResult.Fail("Unreachable");

        OnClearVisuals?.Invoke();
        await _unit.MovePathAsync(result.ValidPath.ToList(), _mapManager);
        return ActionExecutionResult.Ok();
    }
}