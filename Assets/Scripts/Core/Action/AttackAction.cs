using UnityEngine;
using Cysharp.Threading.Tasks;
using System; // [필수] Action 델리게이트 사용
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

        // [개선] ServiceLocator 안전성 확보 (필수 시스템 누락 시 액션 비활성화)
        if (!ServiceLocator.TryGet(out _mapManager))
        {
            Debug.LogError("[AttackAction] Critical: MapManager not found! Action Disabled.");
            State = ActionState.Disabled;
            return;
        }

        if (!ServiceLocator.TryGet(out _combatManager))
        {
            Debug.LogWarning("[AttackAction] CombatManager not found! Attacks may fail.");
        }
    }

    public override string GetActionName() => "Attack";

    // ------------------------------------------------------------------------
    // [Helper Methods] 복잡한 로직 분리 & Null 합병 연산자 활용
    // ------------------------------------------------------------------------

    /// <summary>
    /// 현재 장착된 무기 데이터를 가져옵니다. 없으면 Null.
    /// </summary>
    private WeaponDataSO GetWeapon() => _unit.Data?.MainWeapon;

    /// <summary>
    /// 무기 사거리를 가져옵니다. 무기가 없으면 기본값 1.
    /// </summary>
    private int GetWeaponRange() => GetWeapon()?.Range ?? 1;

    /// <summary>
    /// [SSOT] 무기 제약 조건(Heavy)에 의해 공격이 차단되는지 확인합니다.
    /// </summary>
    private bool IsMovementRestricted()
    {
        // 무기가 없거나, 제약이 Heavy가 아니면 제한 없음
        if (GetWeapon()?.Constraint != ConstraintType.Heavy) return false;

        // Heavy 무기인데 이미 이동했다면 사격 불가
        return _unit.HasStartedMoving;
    }

    /// <summary>
    /// [SSOT] 무기 타입에 따라 공격 후 상태(턴 종료 vs 이동 전환)를 결정합니다.
    /// </summary>
    private ActionConsequence GetActionConsequence()
    {
        // Light 무기는 사격 후 이동 가능 (Hit & Run)
        if (GetWeapon()?.Constraint == ConstraintType.Light)
        {
            Debug.Log("[AttackAction] Light Weapon: Hit & Run enabled");
            return ActionConsequence.SwitchToDefaultAction;
        }

        // Standard, Heavy, 또는 무기 없음 -> 턴 종료
        return ActionConsequence.EndTurn;
    }

    /// <summary>
    /// 두 좌표 사이의 유클리드 거리를 계산합니다. (GridUtils.IsInRange 대체)
    /// </summary>
    private float GetDistance(GridCoords from, GridCoords to)
    {
        float dx = from.x - to.x;
        float dz = from.z - to.z;
        return Mathf.Sqrt((dx * dx) + (dz * dz));
    }

    // ------------------------------------------------------------------------
    // [Core Logic] 실행 가능 여부 판단
    // ------------------------------------------------------------------------

    public override bool CanExecute(GridCoords targetCoords = default)
    {
        if (State == ActionState.Disabled || State == ActionState.Running) return false;

        // 1. 이미 공격했으면 불가
        if (_unit.HasAttacked) return false;

        // 2. [New] 무기 제약 조건 확인 (Heavy Weapon)
        if (IsMovementRestricted()) return false;

        // 3. 타겟 유효성 검사 (입력이 있는 경우만)
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

        // [New] 상세 차단 사유 반환
        if (IsMovementRestricted())
        {
            return "Cannot fire after moving (Heavy Weapon)";
        }

        if (targetCoords != default)
        {
            return GetTargetValidationResult(targetCoords);
        }

        return "";
    }

    // ------------------------------------------------------------------------
    // [Execution] 실행 로직
    // ------------------------------------------------------------------------

    protected override async UniTask<ActionExecutionResult> OnClickAsync(GridCoords mouseGrid, CancellationToken token)
    {
        // 1. 최종 유효성 검사
        string targetError = GetTargetValidationResult(mouseGrid);
        if (!string.IsNullOrEmpty(targetError))
        {
            return ActionExecutionResult.Fail(targetError);
        }

        OnHideRange?.Invoke();

        // 2. 공격 실행
        Unit targetUnit = _mapManager.GetUnit(mouseGrid);
        if (_combatManager != null)
        {
            await _combatManager.ExecuteAttack(_unit, targetUnit);
        }
        else
        {
            return ActionExecutionResult.Fail("Combat System Error");
        }

        // 3. [New] 무기 타입에 따른 결과 반환 (SSOT 적용)
        ActionConsequence consequence = GetActionConsequence();
        return ActionExecutionResult.Ok(consequence);
    }

    // ------------------------------------------------------------------------
    // [Validation] 타겟 검증 로직
    // ------------------------------------------------------------------------

    private string GetTargetValidationResult(GridCoords targetCoords)
    {
        if (targetCoords.Equals(_unit.Coordinate)) return "Cannot attack yourself";

        int range = GetWeaponRange();
        float distance = GetDistance(_unit.Coordinate, targetCoords);

        // [Fix] 거리 정보가 포함된 상세 메시지 반환
        // 유클리드 거리 비교 (약간의 오차 허용을 위해 0.01f 추가 가능)
        if (distance > range + 0.01f)
        {
            return $"Out of Range ({distance:F1}/{range})";
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

    // ------------------------------------------------------------------------
    // [Visuals]
    // ------------------------------------------------------------------------

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

    public override void OnUpdate(GridCoords mouseGrid) { }
}