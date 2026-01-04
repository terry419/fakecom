using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;
using YCOM.Utils;

public class BootManager : MonoBehaviour
{
    public static event Action<bool> OnBootComplete;
    private StringBuilder _bootLog = new StringBuilder();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // [Mod] 탐험 씬 테스트를 위해 자동 전투 진입을 끕니다.
    private const bool AUTO_START_SESSION = false;
#else
    private const bool AUTO_START_SESSION = false;
#endif

    private async void Start() => await BootAsync();

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
            // 1.5. Session Strategy
            // ----------------------------------------------------------------
            _bootLog.AppendLine("\n1.5. Session");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AUTO_START_SESSION)
            {
                try
                {
                    if (!ServiceLocator.TryGet(out MapCatalogManager catalogMgr))
                        throw new BootstrapException("CatalogManager not found.");

                    // [Fix] int(1) -> MissionDifficulty.Normal (Enum)
                    if (!catalogMgr.TryGetRandomMissionByDifficulty(1, out MissionDataSO selectedMission))
                        throw new BootstrapException("No mission found for Difficulty 1.");

                    await gameManager.StartSessionAsync(selectedMission, false);

                    // [Fix] MissionSettings -> Definition
                    _bootLog.AppendLine($"   ✓ Auto-started (Mission: {selectedMission.Definition.MissionName})");
                }
                catch (Exception ex)
                {
                    _bootLog.AppendLine($"   ✗ FAILED: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _bootLog.AppendLine("   - Skipped (Config is false). Waiting for Exploration...");
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
                            // [Fix] MissionSettings -> Definition
                            _bootLog.Append($"   - Loading Map ({currentMission.Definition.MissionName})...");
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

                // [Fix] MapCatalogManager -> MapCatalogSO
                MapCatalogSO catalogSO = null;
                if (ServiceLocator.TryGet(out MapCatalogManager catalogMgr))
                {
                    catalogSO = catalogMgr.GetCatalogSO();
                }

                var sceneContext = new InitializationContext
                {
                    Scope = ManagerScope.Scene,
                    GlobalSettings = globalSettings,
                    Registry = tileRegistry,
                    MapCatalog = catalogSO,
                    MissionData = currentMission,
                    MapData = loadedMapData
                };

                var validation = sceneContext.Validate();
                if (!validation) throw new BootstrapException($"Context Validation Failed: {validation.ErrorMessage}");

                await sceneInitializer.InitializeSceneAsync(sceneContext, _bootLog);
                _bootLog.AppendLine("   ✓ Initialized");
            }
            else
            {
                _bootLog.AppendLine("\n2. Scene Systems");
                _bootLog.AppendLine("   - Warning: SceneInitializer not found. (Exploration Mode?)");
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