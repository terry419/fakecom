using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

public class AttackAction : BaseAction
{
    private MapManager _mapManager;
    private CombatManager _combatManager;

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        _mapManager = ServiceLocator.Get<MapManager>();
        _combatManager = ServiceLocator.Get<CombatManager>();
    }

    public override string GetActionName() => "Attack";

    public override ActionCost GetActionCost() => new ActionCost { AP = 0 };

    // 무기 사거리 가져오기 (없으면 기본값 1)
    private int GetWeaponRange()
    {
        return (_unit.Data != null && _unit.Data.MainWeapon != null)
            ? _unit.Data.MainWeapon.Range : 1;
    }

    public override bool CanExecute(GridCoords targetCoords = default)
    {
        if (State == ActionState.Disabled || State == ActionState.Running) return false;

        // 1. 이미 공격했으면 불가
        if (_unit.HasAttacked) return false;

        // 2. Sniper 이동 후 사격 불가
        if (_unit.ClassType == ClassType.Sniper && _unit.HasStartedMoving)
            return false;

        // 3. 타겟 유효성 체크
        if (targetCoords != default)
        {
            if (!CanAttackTarget(targetCoords)) return false;
        }

        return true;
    }

    // 실패 사유 상세화
    public override string GetBlockReason(GridCoords targetCoords = default)
    {
        if (_unit.HasAttacked) return "Already Attacked";

        if (_unit.ClassType == ClassType.Sniper && _unit.HasStartedMoving)
            return "Cannot fire after moving";

        string baseReason = base.GetBlockReason(targetCoords);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;

        if (targetCoords != default)
        {
            if (targetCoords.Equals(_unit.Coordinate)) return "Cannot attack yourself";

            // 실제 사거리 체크
            int range = GetWeaponRange();
            int distance = GridUtils.GetManhattanDistance(_unit.Coordinate, targetCoords);
            if (distance > range) return $"Out of Range ({distance}/{range})";

            if (!_mapManager.HasUnit(targetCoords)) return "No Valid Target";

            Unit target = _mapManager.GetUnit(targetCoords);
            if (target != null && target.Faction == _unit.Faction) return "Friendly Fire";
        }

        return "";
    }

    private bool CanAttackTarget(GridCoords targetCoords)
    {
        if (targetCoords.Equals(_unit.Coordinate)) return false;

        int range = GetWeaponRange();
        int distance = GridUtils.GetManhattanDistance(_unit.Coordinate, targetCoords);
        if (distance > range) return false;

        if (!_mapManager.HasUnit(targetCoords)) return false;
        Unit target = _mapManager.GetUnit(targetCoords);
        if (target != null && target.Faction == _unit.Faction) return false;

        return true;
    }

    public override void OnUpdate(GridCoords mouseGrid) { }

    protected override async UniTask<ActionExecutionResult> OnClickAsync(GridCoords mouseGrid, CancellationToken token)
    {
        // 1. 실행 전 검증
        string reason = GetBlockReason(mouseGrid);
        if (!string.IsNullOrEmpty(reason))
        {
            Debug.LogWarning($"[AttackAction] Failed: {reason}");
            return ActionExecutionResult.Fail(reason);
        }

        Unit targetUnit = _mapManager.GetUnit(mouseGrid);

        // 2. 전투 매니저 호출 (실제 데미지 처리)
        if (_combatManager != null)
        {
            await _combatManager.ExecuteAttack(_unit, targetUnit);
        }
        else
        {
            Debug.LogError("[AttackAction] CombatManager Missing!");
            return ActionExecutionResult.Fail("System Error");
        }

        return ActionExecutionResult.Ok();
    }
}