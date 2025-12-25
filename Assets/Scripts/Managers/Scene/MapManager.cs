using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
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

    public void Initialize() { } // <-- 추가
    // 타일 조회, 경로 계산(A*) 등의 로직이 들어올 공간입니다.
    public void InitMap()
    {
        Debug.Log("[MapManager] 맵 데이터 초기화 준비.");
    }
}

