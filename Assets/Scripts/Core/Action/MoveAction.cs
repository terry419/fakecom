using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public class MoveAction : BaseAction
{
    private MovementPlanner _planner;
    private PathVisualizer _pathVisualizer;
    private MapManager _mapManager;

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        _mapManager = ServiceLocator.Get<MapManager>();
        _pathVisualizer = ServiceLocator.Get<PathVisualizer>();
        _planner = new MovementPlanner(_mapManager);
    }

    public override string GetActionName() => "Move";

    public override bool CanExecute(GridCoords targetCoords = default)
    {
        if (State == ActionState.Disabled || State == ActionState.Running) return false;

        if (_unit.CurrentMobility <= 0) return false;

        // Ÿ ǥ   ȿ üũ
        if (targetCoords != default)
        {
            var result = _planner.CalculatePath(_unit, targetCoords);
            if (!result.IsValidMovePath) return false;
        }
        return true;
    }

    public override string GetBlockReason(GridCoords targetCoords = default)
    {
        string baseReason = base.GetBlockReason(targetCoords);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;

        if (targetCoords != default)
        {
            var result = _planner.CalculatePath(_unit, targetCoords);
            if (!result.IsValidMovePath) return "Invalid Path";

            if (result.ValidPath.Count == 0 || !result.ValidPath.Last().Equals(targetCoords))
                return "Target Unreachable";
        }
        return string.Empty;
    }

    private void RefreshMoveVisuals()
    {
        _planner.CalculateReachableArea(_unit);
        _pathVisualizer?.ShowReachable(_planner.CachedReachableTiles, _unit.Coordinate);
    }

    public override void OnSelect()
    {
        base.OnSelect();
        if (State == ActionState.Active)
        {
            RefreshMoveVisuals();
        }
    }

    public override void OnExit()
    {
        base.OnExit();
        _pathVisualizer?.ClearAll();
        _planner.InvalidatePathCache();
    }

    public override void OnUpdate(GridCoords mouseGrid)
    {
        if (State != ActionState.Active) return;

        PathCalculationResult result = _planner.CalculatePath(_unit, mouseGrid);
        if (result.HasAnyPath)
        {
            _pathVisualizer?.ShowHybridPath(result.ValidPath.ToList(), result.InvalidPath.ToList());
        }
        else
        {
            _pathVisualizer?.ClearPath();
        }
    }

    protected override async UniTask<ActionExecutionResult> OnClickAsync(GridCoords mouseGrid, CancellationToken token)
    {
        PathCalculationResult result = _planner.CalculatePath(_unit, mouseGrid);

        if (!result.IsValidMovePath)
            return ActionExecutionResult.Fail("Invalid Path Analysis");

        if (result.ValidPath.Count == 0 || !result.ValidPath.Last().Equals(mouseGrid))
            return ActionExecutionResult.Fail("Target Unreachable");

        _pathVisualizer?.ClearAll();

        await _unit.MovePathAsync(result.ValidPath.ToList(), _mapManager);

        return ActionExecutionResult.Ok();
    }
}
