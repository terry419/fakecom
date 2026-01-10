using UnityEngine;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(UnitStatus))]
public class PlayerUnitController : MonoBehaviour, IUnitController
{
    public TeamType Team => TeamType.Player;

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
        Debug.Log($"<color=cyan>[PlayerUnit] {_status.name} 턴 시작.</color>");
        // 매니저(PlayerController)가 있다면 여기서 입력을 기다리거나 신호를 보냄
        await UniTask.CompletedTask;
    }

    public void OnTurnEnd()
    {
        Debug.Log($"[PlayerUnit] {_status.name} 턴 종료.");
    }
}