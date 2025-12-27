using Cysharp.Threading.Tasks;
using UnityEngine;

public class CameraController : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }
    public void Initialize() { }
    // 카메라 리센터, 액션뷰 전환, 쉐이크 효과 등을 관리합니다.
    public void SetTarget()
    {
        Debug.Log("[CameraController] 카메라 타겟 설정.");
    }
}