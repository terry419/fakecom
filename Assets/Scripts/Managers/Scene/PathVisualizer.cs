using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class PathVisualizer : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<PathVisualizer>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PathVisualizer] Error: {ex.Message}");
            throw;
        }
    }

    public void DrawPath() { }
}