using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class SessionManager : MonoBehaviour, IInitializable
{
    private MapEntry? _initialMission; // [New] Initialize 이전에 주입된 미션
    private MapEntry? _currentMissionEntry;
    public MapEntry? CurrentMissionEntry => _currentMissionEntry;

    public bool IsInitialized { get; private set; } = false;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Session);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<SessionManager>())
        {
            ServiceLocator.Unregister<SessionManager>(ManagerScope.Session);
        }
    }

    // [New] GameManager에서 세션 생성 직후(Initialize 전) 호출됨
    public void SetInitialMission(MapEntry entry)
    {
        if (IsInitialized)
        {
            Debug.LogError("[SessionManager] SetInitialMission called AFTER initialization. Ignored.");
            return;
        }
        _initialMission = entry;
    }

    public async UniTask Initialize(InitializationContext context)
    {
        try
        {
            if (context.GlobalSettings == null)
                throw new BootstrapException("[SessionManager] GlobalSettings missing");
            if (context.Registry == null)
                throw new BootstrapException("[SessionManager] TileRegistry missing");

            // 미션 결정 로직: 
            // 1. SetInitialMission으로 주입된 값 (Test Mode)
            // 2. MissionManager에서 가져온 값 (Normal Flow)
            if (_initialMission.HasValue)
            {
                _currentMissionEntry = _initialMission;
                Debug.Log($"[SessionManager] Mission Set (Initial): {_currentMissionEntry.Value.MapID}");
            }
            else if (ServiceLocator.TryGet(out MissionManager missionMgr))
            {
                if (missionMgr.SelectedMission.HasValue)
                {
                    _currentMissionEntry = missionMgr.SelectedMission.Value;
                    Debug.Log($"[SessionManager] Mission Set (Manager): {_currentMissionEntry.Value.MapID}");
                }
            }
            else
            {
                _currentMissionEntry = null;
                Debug.Log("[SessionManager] No Mission Set (Empty Session).");
            }

            IsInitialized = true;
            Debug.Log($"[SessionManager] Initialized.");

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