using UnityEngine;

public class TurnManager : MonoBehaviour, IInitializable
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
        ServiceLocator.Unregister<TurnManager>(this);
        Debug.Log("[Self-Unregister] TurnManager Unregistered.");
    }

    public void Initialize() { }
    // 나중에 턴 순서, 제한 시간 등을 여기서 관리하겠죠?
    public void StartBattle()
    {
        Debug.Log("전투 시작! 턴 시스템 가동.");
    }
}