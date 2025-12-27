using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerInputCoordinator : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }
    public void Initialize() { }
    // 플레이어의 현재 입력 모드(상태)를 제어합니다.
    public void UpdateInputState()
    {
        Debug.Log("[PlayerInputCoordinator] 입력 상태 갱신.");
    }
}