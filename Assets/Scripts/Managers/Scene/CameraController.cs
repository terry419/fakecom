using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class CameraController : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<CameraController>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CameraController] Error: {ex.Message}");
            throw;
        }
    }

    public void SetTarget() { }
}