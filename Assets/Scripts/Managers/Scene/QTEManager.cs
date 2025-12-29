using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class QTEManager : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<QTEManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QTEManager] Error: {ex.Message}");
            throw;
        }
    }

    public void StartQTE() { }
}