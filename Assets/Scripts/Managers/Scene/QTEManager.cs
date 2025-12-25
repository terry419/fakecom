using UnityEngine;

public class QTEManager : MonoBehaviour, IInitializable
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
    // QTE 시작 요청 및 결과 반환 로직이 들어올 공간입니다.
    public void StartQTE()
    {
        Debug.Log("[QTEManager] QTE 시스템 가동.");
    }
}