using UnityEngine;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(UnitStatus))]
public class EnemyUnitController : MonoBehaviour, IUnitController
{
    public TeamType Team => TeamType.Enemy;

    private Unit _unit;
    private UnitStatus _status;

    private void Awake()
    {
        _status = GetComponent<UnitStatus>();
    }

    public async UniTask<bool> Possess(Unit unit)
    {
        _unit = unit;
        _status = unit.Status;
        await UniTask.CompletedTask; // 경고 제거용
        return true;
    }

    public async UniTask Unpossess()
    {
        _unit = null;
        await UniTask.CompletedTask; // 경고 제거용
    }

    public async UniTask OnTurnStart()
    {
        Debug.Log($"<color=red>[EnemyUnit] {_status.name} AI 연산 중...</color>");
        await ThinkingProcess();
    }

    private async UniTask ThinkingProcess()
    {
        await UniTask.Delay(1000); // 1초 생각

        if (ServiceLocator.TryGet<TurnManager>(out var tm))
        {
            tm.EndTurn();
        }
    }

    public void OnTurnEnd()
    {
        Debug.Log($"[EnemyUnit] {_status.name} 턴 종료.");
    }
}