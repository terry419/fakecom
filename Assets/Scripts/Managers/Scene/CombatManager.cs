using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class CombatManager : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<CombatManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CombatManager] Error: {ex.Message}");
            throw;
        }
    }

    public void ExecuteAction() { }
}