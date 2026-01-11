using UnityEngine;
using Cysharp.Threading.Tasks;

// [Refactor] 5. 구체적 클래스에 RequireComponent 선언
[RequireComponent(typeof(UnitStatus))]
public class PlayerUnitController : BaseUnitController
{
    public override TeamType Team => TeamType.Player;

    public override UniTask OnTurnStart()
    {
        // [Refactor] 6. 헬퍼 메서드로 검증 간소화
        if (!ValidateStatus()) return UniTask.CompletedTask;

        // [Refactor] 4. 중앙화된 색상 로그 사용
        Debug.Log($"{ColoredLogTag} {_status.name} 턴 시작.");

        // 매니저(PlayerController)가 있다면 여기서 입력을 기다리거나 신호를 보냄
        return UniTask.CompletedTask;
    }
}