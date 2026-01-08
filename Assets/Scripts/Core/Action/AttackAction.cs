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

        return ActionExecutionResult.Ok();
    }

    private string GetTargetValidationResult(GridCoords targetCoords)
    {
        // 1. 자기 자신 공격 불가 체크
        if (targetCoords.Equals(_unit.Coordinate)) return "Cannot attack yourself";

        // 2. 사거리 계산 (성능 최적화 버전)
        int range = GetWeaponRange();

        // x, z축 차이 계산
        float dx = _unit.Coordinate.x - targetCoords.x;
        float dz = _unit.Coordinate.z - targetCoords.z;

        // 거리의 제곱 (x^2 + z^2) - Sqrt를 사용하지 않아 연산이 빠름
        float sqrDistance = (dx * dx) + (dz * dz);

        // 사거리의 제곱과 비교
        if (sqrDistance > (range * range))
        {
            // 실제 거리 로그는 에러 발생 시에만 계산 (사용자 편의용)
            float actualDistance = Mathf.Sqrt(sqrDistance);
            return $"Out of Range ({actualDistance:F1}/{range})";
        }

        // 3. 타일 내 유닛 존재 여부 확인
        if (!_mapManager.HasUnit(targetCoords)) return "No Valid Target";

        // 4. 피아식별 체크
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