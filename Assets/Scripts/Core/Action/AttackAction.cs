using UnityEngine;
using Cysharp.Threading.Tasks;
using System; // Action 이벤트를 위해 추가
using System.Threading;

public class AttackAction : BaseAction
{
    private MapManager _mapManager;
    private CombatManager _combatManager;

    // [변경] PathVisualizer 제거 -> 이벤트 정의
    public event Action<Vector3, int> OnShowRange;
    public event Action OnHideRange;

    public override void Initialize(Unit unit)
    {
        base.Initialize(unit);
        _mapManager = ServiceLocator.Get<MapManager>();
        _combatManager = ServiceLocator.Get<CombatManager>();
        // _pathVisualizer 제거됨
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
        // [변경] 이벤트 발송
        OnShowRange?.Invoke(_unit.transform.position, GetWeaponRange());
    }

    public override void OnExit()
    {
        base.OnExit();
        // [변경] 이벤트 발송
        OnHideRange?.Invoke();
    }

    public override bool CanExecute(GridCoords targetCoords = default)
    {
        if (State == ActionState.Disabled || State == ActionState.Running) return false;

        // [SSOT] Bridge Property 사용
        if (_unit.HasAttacked) return false;

        if (_unit.ClassType == ClassType.Sniper && _unit.HasStartedMoving)
            return false;

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

        if (_unit.ClassType == ClassType.Sniper && _unit.HasStartedMoving)
            return "Cannot fire after moving";

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

        // [변경] 공격 시작 전 범위 표시 숨기기
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

        return ActionExecutionResult.Ok();
    }

    private string GetTargetValidationResult(GridCoords targetCoords)
    {
        if (targetCoords.Equals(_unit.Coordinate)) return "Cannot attack yourself";

        int range = GetWeaponRange();
        int distance = Mathf.Abs(_unit.Coordinate.x - targetCoords.x) + Mathf.Abs(_unit.Coordinate.z - targetCoords.z);

        if (distance > range) return $"Out of Range ({distance}/{range})";

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