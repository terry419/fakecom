using UnityEngine;

public class CameraController : MonoBehaviour, IInitializable
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
    // 카메라 리센터, 액션뷰 전환, 쉐이크 효과 등을 관리합니다.
    public void SetTarget()
    {
        Debug.Log("[CameraController] 카메라 타겟 설정.");
    }
}