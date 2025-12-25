using UnityEngine;

public class DamageTextManager : MonoBehaviour, IInitializable
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
    // 데미지 텍스트 프리팹 생성 및 연출을 관리합니다.
    public void PopDamageText()
    {
        Debug.Log("[DamageTextManager] 데미지 텍스트 출력.");
    }
}