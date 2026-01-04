using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;
using System.Collections.Generic; // List 사용을 위해 추가
using YCOM.Utils;

public class BootManager : MonoBehaviour
{
    public static event Action<bool> OnBootComplete;
    private StringBuilder _bootLog = new StringBuilder();

    // 상수는 유지하되, 아래 로직에서 전처리기로 분기
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const bool AUTO_START_SESSION = true;
#else
    private const bool AUTO_START_SESSION = false;
#endif

    private async void Start()
    {
        await BootAsync();
    }

    public async UniTask<bool> BootAsync()
    {
        _bootLog.Clear();
        _bootLog.AppendLine("[Boot Sequence Log]");

        try
        {
            // 1. Global Systems
            _bootLog.AppendLine("1. Global Systems");
            await AppBootstrapper.EnsureGlobalSystems();

            if (!ServiceLocator.TryGet(out GameManager gameManager))
                throw new BootstrapException("GameManager not found.");

            _bootLog.AppendLine("   ✓ Initialized");

            // ----------------------------------------------------------------
            // 1.5. Session (Auto Start)
            // ----------------------------------------------------------------
            _bootLog.AppendLine("\n1.5. Session");

            // [Fix] CS0162 경고 해결: 컴파일러가 코드를 아예 제외하도록 전처리기 사용
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AUTO_START_SESSION)
            {
                try
                {
                    if (!ServiceLocator.TryGet(out MapCatalogManager catalog))
                        throw new BootstrapException("Catalog not found.");

                    if (!catalog.TryGetRandomMapByDifficulty(1, out MapEntry mapEntry))
                        throw new BootstrapException("No map found in Catalog.");

                    var dummyMission = MissionDataFactory.CreateTestMission(mapEntry);

                    // true: ownsMission (GameManager가 파괴 책임)
                    await gameManager.StartSessionAsync(dummyMission, true);
                    _bootLog.AppendLine($"   ✓ Auto-started (Mission: {dummyMission.MissionSettings.MissionName})");
                }
                catch (Exception ex)
                {
                    _bootLog.AppendLine($"   ✗ FAILED: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _bootLog.AppendLine("   - Skipped (Config is false)");
            }
#else
            _bootLog.AppendLine("   - Skipped (Release Build)");
#endif

            // ----------------------------------------------------------------
            // 2. Scene Systems
            // ----------------------------------------------------------------
            var sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer != null)
            {
                _bootLog.AppendLine("\n2. Scene Systems");

                var globalSettings = gameManager.GetGlobalSettings();
                var tileRegistry = gameManager.GetTileRegistry();
                MissionDataSO currentMission = null;
                MapDataSO loadedMapData = null;

                if (ServiceLocator.TryGet(out SessionManager sessionMgr) && sessionMgr.IsInitialized)
                {
                    currentMission = sessionMgr.CurrentMission;

                    if (currentMission != null)
                    {
                        try
                        {
                            _bootLog.Append($"   - Loading Map ({currentMission.MissionSettings.MissionName})...");
                            loadedMapData = await MapDataLoader.LoadMapDataAsync(currentMission);
                            _bootLog.AppendLine(" OK");
                        }
                        catch (Exception ex)
                        {
                            _bootLog.AppendLine($" FAILED: {ex.Message}");
                            throw;
                        }
                    }
                }

                var sceneContext = new InitializationContext
                {
                    Scope = ManagerScope.Scene,
                    GlobalSettings = globalSettings,
                    Registry = tileRegistry,
                    MissionData = currentMission,
                    MapData = loadedMapData
                };

                // 검증
                var validation = sceneContext.Validate();
                if (!validation)
                {
                    throw new BootstrapException($"Context Validation Failed: {validation.ErrorMessage}");
                }

                await sceneInitializer.InitializeSceneAsync(sceneContext, _bootLog);
                _bootLog.AppendLine("   ✓ Initialized");
            }
            else
            {
                _bootLog.AppendLine("\n2. Scene Systems");
                _bootLog.AppendLine("   - Warning: SceneInitializer not found.");
            }

            _bootLog.AppendLine("\n<color=green>✓ ALL SYSTEMS READY</color>");
            Debug.Log(_bootLog.ToString());

            OnBootComplete?.Invoke(true);
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red>[BOOT FAILED]</color>\n{_bootLog}\n\nError: {ex.Message}\n{ex.StackTrace}");
            OnBootComplete?.Invoke(false);
            return false;
        }
    }
}