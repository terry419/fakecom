using UnityEngine;
using Cysharp.Threading.Tasks;

public class InputManager : MonoBehaviour, IInitializable
{
    private bool _inputEnabled = true;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<InputManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 입력 시스템 초기화
        EnableInput(true);
        await UniTask.CompletedTask;
    }

    public void EnableInput(bool enabled)
    {
        _inputEnabled = enabled;
        // 나중에 유니티 New Input System 코드가 여기에 들어갑니다.
    }
}