using UnityEngine;
using Cysharp.Threading.Tasks;

public class InputManager : MonoBehaviour, IInitializable
{
    private bool _inputEnabled = true;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
        Debug.Log($"[InputManager] 등록 완료 (Global).");
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 입력 시스템 초기화
        EnableInput(true);
        Debug.Log("[InputManager] 입력 시스템 준비 완료.");
        await UniTask.CompletedTask;
    }

    public void EnableInput(bool enabled)
    {
        _inputEnabled = enabled;
        // 나중에 유니티 New Input System 코드가 여기에 들어갑니다.
    }
}