using UnityEngine;

public class CombatManager : MonoBehaviour, IInitializable
{
    private void Awake()
    {
        // 1. 깨어날 때 스스로를 등록
        ServiceLocator.Register(this);
        Debug.Log("[Self-Register] TurnManager Registered.");
    }


    private void OnDestroy()
    {
        // 2. 파괴될 때 스스로를 등록 해제 (매우 중요!)
        ServiceLocator.Unregister(this);
        Debug.Log("[Self-Unregister] TurnManager Unregistered.");
    }

    public void Initialize() { }
    // 명중률 계산, 데미지 처리, 액션캠 연출 등을 관리합니다.
    public void ExecuteAction()
    {
        Debug.Log("[CombatManager] 전투 액션 실행 로직.");
    }
}