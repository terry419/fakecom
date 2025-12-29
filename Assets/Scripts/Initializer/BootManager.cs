using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

// [Refactoring Phase 1] P0: ServiceLocator 기반 부트스트래퍼
public class BootManager : MonoBehaviour
{
    // 외부에서 부팅 완료를 구독할 수 있는 이벤트
    public static event Action<bool> OnBootComplete;

    private const string PREFIX = "[BootManager]";

    private async void Start()
    {
        await BootAsync();
    }

    // BootManager.cs 내부
    public async UniTask<bool> BootAsync()
    {
        try
        {
            LogInfo("System Boot Started...");

            // ------------------------------------------------------------
            // Phase 1: Core Data (핵심 데이터 로드)
            // ------------------------------------------------------------
            // 병렬 처리를 원한다면 UniTask.WhenAll 사용 가능
            // 현재는 안정성을 위해 순차 실행 구조 유지
            await InitializeManager<DataManager>("DataManager");
            await InitializeManager<EdgeDataManager>("EdgeDataManager");
            await InitializeManager<TileDataManager>("TileDataManager");

            // ------------------------------------------------------------
            // Phase 2: Scene Construction (맵 구성)
            // ------------------------------------------------------------
            await InitializeManager<MapManager>("MapManager");
            await InitializeManager<TilemapGenerator>("TilemapGenerator");

            // ------------------------------------------------------------
            // Phase 3: Runtime Systems (인게임 시스템 활성화)
            // ------------------------------------------------------------
            // [기존 목록]
            await InitializeManagerOptional<CameraController>("CameraController");
            await InitializeManagerOptional<TurnManager>("TurnManager");

            // [추가된 나머지 매니저 목록]
            await InitializeManagerOptional<CombatManager>("CombatManager");
            await InitializeManagerOptional<PathVisualizer>("PathVisualizer");
            await InitializeManagerOptional<PlayerInputCoordinator>("PlayerInputCoordinator");
            await InitializeManagerOptional<TargetUIManager>("TargetUIManager");
            await InitializeManagerOptional<QTEManager>("QTEManager");
            await InitializeManagerOptional<DamageTextManager>("DamageTextManager");

            LogInfo("Boot Sequence Complete. Game is Ready.");
            OnBootComplete?.Invoke(true);
            return true;
        }
        catch (Exception ex)
        {
            LogError($"CRITICAL FAILURE: {ex.Message}");
            OnBootComplete?.Invoke(false);
            return false;
        }
    }
    
    // 필수 매니저 초기화 (없으면 부팅 중단)
    private async UniTask InitializeManager<T>(string name) where T : IInitializable
    {
        // GDD 규칙에 따라 매니저는 Awake에서 이미 Register되어 있어야 함
        var manager = ServiceLocator.Get<T>();

        if (manager == null)
            throw new NullReferenceException($"Critical Dependency '{name}' is missing in ServiceLocator! Verify Awake() registration.");

        await manager.Initialize(new InitializationContext());
    }

    // 선택적 매니저 초기화 (없어도 부팅 계속)
    private async UniTask InitializeManagerOptional<T>(string name) where T : IInitializable
    {
        var manager = ServiceLocator.Get<T>();
        if (manager == null)
        {
            LogWarning($"Optional System '{name}' not found. Skipping.");
            return;
        }

        await manager.Initialize(new InitializationContext());
    }

    #region Logging Helpers
    private void LogInfo(string msg) => Debug.Log($"{PREFIX} {msg}");
    private void LogPhase(int n, string desc) => Debug.Log($"{PREFIX} [Phase {n}] {desc}");
    private void LogSuccess(string msg) => Debug.Log($"{PREFIX}  {msg}");
    private void LogWarning(string msg) => Debug.LogWarning($"{PREFIX}  {msg}");
    private void LogError(string msg) => Debug.LogError($"{PREFIX}  {msg}");
    #endregion
}