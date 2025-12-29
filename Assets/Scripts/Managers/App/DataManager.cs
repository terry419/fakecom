using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class DataManager : MonoBehaviour, IInitializable
{
    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<DataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            // TODO: 세이브 파일 로드 등
            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[DataManager] Error: {ex.Message}");
            throw;
        }
    }
}