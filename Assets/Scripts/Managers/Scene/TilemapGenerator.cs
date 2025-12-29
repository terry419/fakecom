using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class TilemapGenerator : MonoBehaviour, IInitializable
{
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<TilemapGenerator>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TilemapGenerator] Error: {ex.Message}");
            throw;
        }
    }

    public void Generate() { Debug.Log("Generate Map"); }
}