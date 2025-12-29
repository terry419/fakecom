using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class PlayerInputCoordinator : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<PlayerInputCoordinator>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerInputCoordinator] Error: {ex.Message}");
            throw;
        }
    }

    public void UpdateInputState() { }
}