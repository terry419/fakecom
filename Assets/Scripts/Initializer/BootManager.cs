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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AUTO_START_SESSION)
            {
                try
                {
                    if (!ServiceLocator.TryGet(out MapCatalogManager catalogMgr))
                        throw new BootstrapException("CatalogManager not found.");

                    // [Fix] Factory 삭제 -> CatalogManager에서 진짜 미션 뽑아오기
                    if (!catalogMgr.TryGetRandomMissionByDifficulty(1, out MissionDataSO selectedMission))
                        throw new BootstrapException("No mission found for Difficulty 1.");

                    // [Fix] 세션 시작 (ownsMission = false, 에셋 보호)
                    await gameManager.StartSessionAsync(selectedMission, false);

                    _bootLog.AppendLine($"   ✓ Auto-started (Mission: {selectedMission.MissionSettings.MissionName})");
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

                // [Fix] MapCatalogManager를 가져와서 SO를 꺼냄 (컴파일 에러 해결)
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
                    MapCatalog = catalogSO, // [Fix] 가져온 CatalogSO 주입
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