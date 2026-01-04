using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;

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
            // 1.5. Session (Test Mode Map Selection)
            // ----------------------------------------------------------------
            _bootLog.AppendLine("\n1.5. Session");
            if (AUTO_START_SESSION)
            {
                try
                {
                    // [Step 1] 테스트 맵 찾기
                    if (!ServiceLocator.TryGet(out MapCatalogManager catalog))
                        throw new BootstrapException("MapCatalogManager not found.");

                    if (!catalog.TryGetRandomMapByDifficulty(1, out MapEntry testMap))
                        throw new BootstrapException("No test map found in Catalog.");

                    // [Step 2] 세션 시작 (맵 주입)
                    await gameManager.StartSessionAsync(testMap);
                    _bootLog.AppendLine($"   ✓ Auto-started (Test Map: {testMap.MapID})");
                }
                catch (Exception ex)
                {
                    _bootLog.AppendLine($"   ✗ FAILED: {ex.Message}");
                    throw;
                }
            }
            else
            {
                _bootLog.AppendLine("   - Skipped (Manual Start)");
            }

            // ----------------------------------------------------------------
            // 2. Scene Systems
            // ----------------------------------------------------------------
            var sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer != null)
            {
                _bootLog.AppendLine("\n2. Scene Systems");

                var globalSettings = gameManager.GetGlobalSettings();
                var tileRegistry = gameManager.GetTileRegistry();

                MapEntry? currentMission = null;
                MapDataSO loadedMapData = null;

                // [Data Prep] 세션에서 미션 정보 가져오기 & MapData 로드
                if (ServiceLocator.TryGet(out SessionManager sessionMgr) && sessionMgr.IsInitialized)
                {
                    currentMission = sessionMgr.CurrentMissionEntry;

                    if (currentMission.HasValue)
                    {
                        try
                        {
                            // [핵심] BootManager가 책임을 지고 에셋을 로드함
                            _bootLog.Append($"   - Loading MapData ({currentMission.Value.MapID})...");
                            loadedMapData = await currentMission.Value.MapDataRef.LoadAssetAsync();
                            _bootLog.AppendLine(" OK");
                        }
                        catch (Exception ex)
                        {
                            _bootLog.AppendLine($" FAILED: {ex.Message}");
                            throw;
                        }
                    }
                }

                // [Context Create] 완성된 데이터 패키징
                var sceneContext = new InitializationContext
                {
                    Scope = ManagerScope.Scene,
                    GlobalSettings = globalSettings,
                    Registry = tileRegistry,
                    SelectedMission = currentMission,
                    MapData = loadedMapData // 이제 null이 아님!
                };

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