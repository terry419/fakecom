using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class GameManager : MonoBehaviour, IInitializable
{
    public bool IsPaused { get; private set; } = false;
    private GameObject _sessionRoot;

    private MissionDataSO _runtimeMission;
    private bool _ownsRuntimeMission;
    private HideFlags _originalMissionHideFlags; // [Fix] 복구용 저장소

    private GlobalSettingsSO _globalSettings;
    private TileRegistrySO _tileRegistry;

    public GlobalSettingsSO GetGlobalSettings() => _globalSettings ?? throw new InvalidOperationException("GM Not Init");
    public TileRegistrySO GetTileRegistry() => _tileRegistry ?? throw new InvalidOperationException("GM Not Init");

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);
    private void OnDestroy() => ServiceLocator.Unregister<GameManager>(ManagerScope.Global);

    public async UniTask Initialize(InitializationContext context)
    {
        _globalSettings = context.GlobalSettings;
        _tileRegistry = context.Registry;
        if (_globalSettings != null) Application.targetFrameRate = _globalSettings.TargetFrameRate;
        await UniTask.CompletedTask;
    }

    public async UniTask<bool> StartSessionAsync(MissionDataSO missionData = null, bool ownsMission = false)
    {
        if (_sessionRoot != null)
        {
            Debug.LogWarning("[GameManager] Session already running.");
            return false;
        }

        _runtimeMission = missionData;
        _ownsRuntimeMission = ownsMission;

        // [Fix] HideFlags 저장 및 설정
        if (_runtimeMission != null)
        {
            _originalMissionHideFlags = _runtimeMission.hideFlags;
            if (_ownsRuntimeMission)
            {
                _runtimeMission.hideFlags = HideFlags.DontSave;
            }
        }

        Debug.Log("[GameManager] Starting new session...");
        _sessionRoot = new GameObject("@SessionSystems");
        _sessionRoot.transform.SetParent(this.transform);

        var sessionMgr = _sessionRoot.AddComponent<SessionManager>();

        var sessionContext = new InitializationContext
        {
            Scope = ManagerScope.Session,
            GlobalSettings = _globalSettings,
            Registry = _tileRegistry,
            MissionData = missionData
        };

        try
        {
            await sessionMgr.Initialize(sessionContext);
            Debug.Log("[GameManager] Session Started successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] Session Init Failed: {ex.Message}");
            await EndSessionAsync();
            throw;
        }
    }

    public async UniTask EndSessionAsync()
    {
        if (_sessionRoot != null)
        {
            Destroy(_sessionRoot);
            _sessionRoot = null;
            await UniTask.NextFrame();
        }

        await ServiceLocator.ClearScopeAsync(ManagerScope.Session);

        CleanupRuntimeMission();

        Debug.Log("[GameManager] Session Ended.");
    }

    private void CleanupRuntimeMission()
    {
        if (_runtimeMission != null)
        {
            if (_ownsRuntimeMission)
            {
                // 소유권이 있으면 파괴
                if (_runtimeMission.EnemySpawns != null)
                {
                    foreach (var spawn in _runtimeMission.EnemySpawns)
                    {
                        // [Fix] spawn.Unit -> spawn.UnitData 로 수정
                        SafeDestroy(spawn.UnitData);
                    }
                }

                // 3. 중립 데이터 정리
                if (_runtimeMission.NeutralSpawns != null)
                {
                    foreach (var spawn in _runtimeMission.NeutralSpawns)
                    {
                        // [Fix] spawn.Unit -> spawn.UnitData 로 수정
                        SafeDestroy(spawn.UnitData);
                    }
                }
            }
            else
            {
                // [Fix] 소유권이 없으면 HideFlags 원복
                _runtimeMission.hideFlags = _originalMissionHideFlags;
            }
        }
        _runtimeMission = null;
    }

    private void SafeDestroy(UnityEngine.Object obj)
    {
        if (obj == null) return;
#if UNITY_EDITOR
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
#else
        Destroy(obj);
#endif
    }

    public void StartGame() { IsPaused = false; Time.timeScale = 1.0f; }
    public void PauseGame() { IsPaused = true; Time.timeScale = 0.0f; }
}