using UnityEngine;

public class TilemapGenerator : MonoBehaviour, IInitializable
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
    // 설정된 크기대로 타일을 스폰하고 관리합니다.
    public void Generate()
    {
        Debug.Log("[TilemapGenerator] 타일맵 생성 실행.");
    }
}