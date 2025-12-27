using Cysharp.Threading.Tasks;
using UnityEngine;

public class CombatManager : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }
    public void Initialize() { }
    // 명중률 계산, 데미지 처리, 액션캠 연출 등을 관리합니다.
    public void ExecuteAction()
    {
        Debug.Log("[CombatManager] 전투 액션 실행 로직.");
    }
}