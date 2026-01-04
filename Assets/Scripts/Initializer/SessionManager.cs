using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class SessionManager : MonoBehaviour, IInitializable
{
    private MissionDataSO _currentMissionData;
    public MissionDataSO CurrentMission => _currentMissionData;
    public bool IsInitialized { get; private set; } = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Session);
    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<SessionManager>())
            ServiceLocator.Unregister<SessionManager>(ManagerScope.Session);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            var validation = context.Validate();
            if (!validation)
            {
                throw new BootstrapException($"[SessionManager] Validation Failed: {validation.ErrorMessage}");
            }

            _currentMissionData = context.MissionData;
            IsInitialized = true;

            Debug.Log($"[SessionManager] Initialized. Mission: {_currentMissionData.MissionSettings.MissionName}");

            await UniTask.CompletedTask;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SessionManager] Init Failed: {ex.Message}");
            IsInitialized = false;
            throw;
        }
    }
}