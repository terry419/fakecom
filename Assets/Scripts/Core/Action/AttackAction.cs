using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public class AttackAction : BaseAction
{
    private MapManager _mapManager;
    private CombatManager _combatManager;

    public event Action<Vector3, int> OnShowRange;
    public event Action OnHideRange;

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        _mapManager = ServiceLocator.Get<MapManager>();
        _combatManager = ServiceLocator.Get<CombatManager>();
    }

    public override string GetActionName() => "Attack";

    private int GetWeaponRange()
    {
        return (_unit.Data != null && _unit.Data.MainWeapon != null)
            ? _unit.Data.MainWeapon.Range : 1;
    }

    public override void OnSelect()
    {
        base.OnSelect();
        OnShowRange?.Invoke(_unit.transform.position, GetWeaponRange());
    }

    public override void OnExit()
    {
        base.OnExit();
        OnHideRange?.Invoke();
    }

    public override bool CanExecute(GridCoords targetCoords = default)
    {
        if (State == ActionState.Disabled || State == ActionState.Running) return false;
        if (_unit.HasAttacked) return false;
        if (_unit.ClassType == ClassType.Sniper && _unit.HasStartedMoving) return false;

        if (targetCoords != default)
        {
            if (!CanAttackTarget(targetCoords)) return false;
        }

        return true;
    }

    public override string GetBlockReason(GridCoords targetCoords = default)
    {
        string baseReason = base.GetBlockReason(targetCoords);
        if (!string.IsNullOrEmpty(baseReason)) return baseReason;

        if (_unit.HasAttacked) return "Already Attacked";
        if (_unit.ClassType == ClassType.Sniper && _unit.HasStartedMoving) return "Cannot fire after moving";

        if (targetCoords != default)
        {
            return GetTargetValidationResult(targetCoords);
        }

        return "";
    }

    protected override async UniTask<ActionExecutionResult> OnClickAsync(GridCoords mouseGrid, CancellationToken token)
    {
        string targetError = GetTargetValidationResult(mouseGrid);
        if (!string.IsNullOrEmpty(targetError))
        {
            Debug.LogWarning($"[AttackAction] Failed: {targetError}");
            return ActionExecutionResult.Fail(targetError);
        }

        OnHideRange?.Invoke();

        Unit targetUnit = _mapManager.GetUnit(mouseGrid);

        if (_combatManager != null)
        {
            await _combatManager.ExecuteAttack(_unit, targetUnit);
        }
        else
        {
            Debug.LogError("[AttackAction] CombatManager Missing!");
            return ActionExecutionResult.Fail("System Error");
        }

        // [Logic Moved] 클래스별 규칙을 여기서 처리 (Controller에서 제거됨)
        ActionConsequence consequence = ActionConsequence.EndTurn; // 기본: 공격 후 턴 종료

        // 예외: Scout는 공격 후 이동 가능 (Hit & Run)
        if (_unit.ClassType == ClassType.Scout)
        {
            Debug.Log("[AttackAction] Scout Bonus: Switch to Default Action (Hit & Run)");
            consequence = ActionConsequence.SwitchToDefaultAction;
        }

        return ActionExecutionResult.Ok(consequence);
    }

    private string GetTargetValidationResult(GridCoords targetCoords)
    {
        if (targetCoords.Equals(_unit.Coordinate)) return "Cannot attack yourself";

        int range = GetWeaponRange();
        float dx = _unit.Coordinate.x - targetCoords.x;
        float dz = _unit.Coordinate.z - targetCoords.z;
        float sqrDistance = (dx * dx) + (dz * dz);

        if (sqrDistance > (range * range))
        {
            float actualDistance = Mathf.Sqrt(sqrDistance);
            return $"Out of Range ({actualDistance:F1}/{range})";
        }

        if (!_mapManager.HasUnit(targetCoords)) return "No Valid Target";

        Unit target = _mapManager.GetUnit(targetCoords);
        if (target != null && target.Faction == _unit.Faction)
        {
            return "Friendly Fire";
        }

        return "";
    }

    private bool CanAttackTarget(GridCoords targetCoords)
    {
        return string.IsNullOrEmpty(GetTargetValidationResult(targetCoords));
    }

    public override void OnUpdate(GridCoords mouseGrid) { }
}