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

    // [Fix] SetInitialMission 삭제 (Context로 단일화)

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            // [Fix] Context.Validate()를 통한 중앙 집중식 검증
            var validation = context.Validate();
            if (!validation)
            {
                throw new BootstrapException($"[SessionManager] Validation Failed: {validation.ErrorMessage}");
            }

            // Context에서 데이터 확정
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