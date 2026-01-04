using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class GameManager : MonoBehaviour, IInitializable
{
    // ... (기존 변수 및 Safe Getter 유지) ...
    public bool IsPaused { get; private set; } = false;
    private GameObject _sessionRoot;
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

    // [변경] TestMission 파라미터 추가
    public async UniTask StartSessionAsync(MapEntry? testMission = null)
    {
        if (_sessionRoot != null)
        {
            Debug.LogWarning("[GameManager] Session already running.");
            return;
        }

        var settings = GetGlobalSettings();
        var registry = GetTileRegistry();

        Debug.Log("[GameManager] Starting new session...");

        _sessionRoot = new GameObject("@SessionSystems");
        _sessionRoot.transform.SetParent(this.transform);

        var sessionMgr = _sessionRoot.AddComponent<SessionManager>();

        // [핵심] Initialize 전에 미션 주입
        if (testMission.HasValue)
        {
            sessionMgr.SetInitialMission(testMission.Value);
        }

        var sessionContext = new InitializationContext
        {
            Scope = ManagerScope.Session,
            GlobalSettings = settings,
            Registry = registry
        };

        try
        {
            await sessionMgr.Initialize(sessionContext);
            Debug.Log("[GameManager] Session Started successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameManager] Session initialization failed: {ex.Message}");
            Destroy(_sessionRoot);
            _sessionRoot = null;
            throw;
        }
    }

    // ... (EndSessionAsync 및 기타 메서드 유지) ...
    public async UniTask EndSessionAsync()
    {
        if (_sessionRoot != null)
        {
            Destroy(_sessionRoot);
            _sessionRoot = null;
            await UniTask.NextFrame();
        }
        await ServiceLocator.ClearScopeAsync(ManagerScope.Session);
    }
}