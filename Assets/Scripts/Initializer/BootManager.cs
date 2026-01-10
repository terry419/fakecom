using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;
using YCOM.Utils;

public class BootManager : MonoBehaviour
{
    // [New] Race Condition 방지용 정적 이벤트/프로퍼티
    public static event Action<bool> OnBootComplete;
    public static bool IsBootComplete { get; private set; } = false;

    private StringBuilder _bootLog = new StringBuilder();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const bool AUTO_START_SESSION = false;
#else
    private const bool AUTO_START_SESSION = false;
#endif

    private async void Start() => await BootAsync();

    public async UniTask<bool> BootAsync()
    {
        _bootLog.Clear();
        _bootLog.AppendLine("[Boot Sequence Log]");
        bool isSuccess = false; // 성공 여부 추적

        try
        {
            // 초기화 시작 전 플래그 리셋
            IsBootComplete = false;

            // 1. Global Systems
            _bootLog.AppendLine("1. Global Systems");
            await AppBootstrapper.EnsureGlobalSystems();

            if (!ServiceLocator.TryGet(out GameManager gameManager))
                throw new BootstrapException("GameManager not found.");

            _bootLog.AppendLine("   ✓ Initialized");

            // ----------------------------------------------------------------
            // 1.5. Session Strategy (기존 로직 유지)
            // ----------------------------------------------------------------
            _bootLog.AppendLine("\n1.5. Session");

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (AUTO_START_SESSION)
            {
                try
                {
                    if (!ServiceLocator.TryGet(out MapCatalogManager catalogMgr))
                        throw new BootstrapException("CatalogManager not found.");

                    // [Fix] int(1) -> MissionDifficulty.Normal (Enum) 대응 필요 시 수정
                    if (!catalogMgr.TryGetRandomMissionByDifficulty(1, out MissionDataSO selectedMission))
                        throw new BootstrapException("No mission found for Difficulty 1.");

                    await gameManager.StartSessionAsync(selectedMission, false);

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
            // 2. Scene Systems (기존 로직 유지 - 맵 로딩 포함)
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
                            _bootLog.Append($"   - Loading Map ({currentMission.Definition.MissionName})...");
                            // [Restore] 맵 데이터 비동기 로드 복구
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

            // [New] 부팅 완료 처리 (BattleManager 등에서 대기 가능하도록)
            IsBootComplete = true;
            OnBootComplete?.Invoke(true);
            isSuccess = true;

            // 씬 전환 로직
            string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentSceneName == "BootScene" || currentSceneName == "Boot")
            {
                Debug.Log("[BootManager] Boot Sequence Complete. Loading BaseScene...");
                UnityEngine.SceneManagement.SceneManager.LoadScene("BaseScene");
            }
            else
            {
                Debug.Log($"[BootManager] Boot Complete. Detected Test Mode in '{currentSceneName}'. Staying in current scene.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"<color=red>[BOOT FAILED]</color>\n{_bootLog}\n\nError: {ex.Message}\n{ex.StackTrace}");
            IsBootComplete = false;
            OnBootComplete?.Invoke(false);
            isSuccess = false;
        }

        // [Fix CS0162] 모든 경로에서 도달 가능한 리턴
        return isSuccess;
    }
}