using UnityEngine;
using Cysharp.Threading.Tasks;

public abstract class BaseUnitController : MonoBehaviour, IUnitController
{
    protected Unit _possessedUnit;
    protected UnitStatus _status;

    // 자식 클래스에서 오버라이드 가능
    public abstract TeamType Team { get; }

    public virtual async UniTask<bool> Possess(Unit unit)
    {
        if (unit == null) return false;

        _possessedUnit = unit;
        _status = unit.GetComponent<UnitStatus>();

        // 유닛에게 "내가 네 주인이다"라고 알림
        // (Unit.SetController 내부의 재귀 호출 방지를 위해 직접 할당이 아닌 Setter 이용 시 주의)
        if (unit.Controller != this)
        {
            unit.SetController(this);
        }

        Debug.Log($"[{GetType().Name}] Possessed {unit.name}");
        return await UniTask.FromResult(true);
    }

    public virtual async UniTask Unpossess()
    {
        _possessedUnit = null;
        _status = null;
        await UniTask.CompletedTask;
    }

    // 자식 클래스에서 AI 또는 입력 로직 구현
    public abstract UniTask OnTurnStart();

    public virtual void OnTurnEnd()
    {
        // 기본적으로 아무것도 안 함 (AI는 스스로 TurnManager를 호출)
    }

    // 헬퍼: 상태 유효성 검사
    protected bool ValidateStatus()
    {
        if (_possessedUnit == null || _status == null)
        {
            Debug.LogError($"[{GetType().Name}] No unit possessed or status missing.");
            return false;
        }
        if (_status.IsDead)
        {
            Debug.Log($"[{GetType().Name}] Unit is dead. Skipping turn.");
            return false;
        }
        return true;
    }

    protected string ColoredLogTag => $"<color=red>[{GetType().Name}]</color>";
}