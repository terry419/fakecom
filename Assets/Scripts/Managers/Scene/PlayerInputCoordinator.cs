using UnityEngine;

public class PlayerInputCoordinator : MonoBehaviour, IInitializable
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
    // 플레이어의 현재 입력 모드(상태)를 제어합니다.
    public void UpdateInputState()
    {
        Debug.Log("[PlayerInputCoordinator] 입력 상태 갱신.");
    }
}