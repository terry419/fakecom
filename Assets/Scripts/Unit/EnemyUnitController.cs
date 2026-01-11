using UnityEngine;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(UnitStatus))]
public class EnemyUnitController : BaseUnitController
{
    public override TeamType Team => TeamType.Enemy;

    public override async UniTask OnTurnStart()
    {
        if (!ValidateStatus())
        {
            // 유닛이 죽었거나 문제 있으면 즉시 턴 넘김
            EndTurnSafe();
            return;
        }

        Debug.Log($"{ColoredLogTag} {_possessedUnit.name} (AI) Thinking...");

        // AI 로직 실행
        await ThinkingProcess();
    }

    private async UniTask ThinkingProcess()
    {
        // [임시] 1초간 고민하는 척 (추후 여기에 Move/Attack 로직 추가)
        await UniTask.Delay(1000);

        Debug.Log($"{ColoredLogTag} {_possessedUnit.name} (AI) Turn Finished.");

        // 턴 종료
        EndTurnSafe();
    }

    private void EndTurnSafe()
    {
        if (ServiceLocator.TryGet<TurnManager>(out var tm))
        {
            tm.EndTurn();
        }
        else
        {
            Debug.LogError($"{ColoredLogTag} Critical Error: TurnManager not found!");
        }
    }
}