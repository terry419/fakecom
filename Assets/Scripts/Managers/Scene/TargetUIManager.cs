using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class TargetUIManager : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<TargetUIManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TargetUIManager] Error: {ex.Message}");
            throw;
        }
    }

    public void ShowTargetInfo() { }
}