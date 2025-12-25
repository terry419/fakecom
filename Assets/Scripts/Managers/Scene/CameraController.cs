using UnityEngine;

public class CameraController : MonoBehaviour, IInitializable
{
    public void Initialize() { }
    // 카메라 리센터, 액션뷰 전환, 쉐이크 효과 등을 관리합니다.
    public void SetTarget()
    {
        Debug.Log("[CameraController] 카메라 타겟 설정.");
    }
}