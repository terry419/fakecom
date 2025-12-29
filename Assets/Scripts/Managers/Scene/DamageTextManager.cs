using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class DamageTextManager : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<DamageTextManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DamageTextManager] Error: {ex.Message}");
            throw;
        }
    }

    public void PopDamageText() { }
}