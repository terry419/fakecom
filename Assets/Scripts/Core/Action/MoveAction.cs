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
        _planner.CalculateReachableArea(_unit);
        if (OnShowReachable != null)
        {
            OnShowReachable.Invoke(_planner.CachedReachableTiles, _unit.Coordinate);
        }
    }

    public override void OnSelect()
    {
        base.OnSelect();
        // 활성화 시 이동 가능 범위 시각화
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

        // 실제 이동 처리
        await _unit.MovePathAsync(result.ValidPath.ToList(), _mapManager);

        // [Logic Moved] 이동 후 상태 판단 로직

        // 1. 행동력이 남음 -> 계속 이동 가능 (재선택 효과)
        if (_unit.CurrentMobility > 0)
        {
            return ActionExecutionResult.Ok(ActionConsequence.SwitchToDefaultAction);
        }

        // 2. 이동력 소진 & 공격도 완료함 -> 턴 종료
        if (_unit.HasAttacked)
        {
            return ActionExecutionResult.Ok(ActionConsequence.EndTurn);
        }

        // 3. 이동력만 소진 -> 대기 (공격 등 다른 액션 가능)
        return ActionExecutionResult.Ok(ActionConsequence.None);
    }
}