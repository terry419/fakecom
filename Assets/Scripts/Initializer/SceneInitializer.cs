using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;

public class SceneInitializer : MonoBehaviour
{
    [Header("Editor Test Config")]
    [Tooltip("에디터에서 바로 시작할 때 사용할 테스트용 미션 데이터")]
    [SerializeField] private MissionDataSO _testMissionData;

    private async void Start()
    {
        // 1. [정상 루트] BootManager나 GameManager를 통해 이미 시스템이 로드된 상태인지 확인
        if (ServiceLocator.IsRegistered<GameManager>())
        {
            return;
        }

        // 2. [테스트 루트] GameManager가 없다면, 개발자가 이 씬에서 바로 Play를 누른 것입니다.
#if UNITY_EDITOR
        Debug.LogWarning($"<color=yellow>[SceneInitializer] 단독 실행 감지! 글로벌 시스템 부팅 및 세션 시작을 요청합니다...</color>");

        try
        {
            // A. 글로벌 시스템(매니저들) 강제 로드
            await AppBootstrapper.EnsureGlobalSystems();

            // B. 이제 GameManager가 존재하므로 가져옵니다.
            var gameManager = ServiceLocator.Get<GameManager>();

            // C. 테스트용 미션 데이터가 있는지 확인
            MissionDataSO missionToRun = _testMissionData;

            // D. GameManager에게 세션 시작 요청
            await gameManager.StartSessionAsync(missionToRun, false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneInitializer] Self-Boot Error: {ex.Message}\n{ex.StackTrace}");
        }
#else
        Debug.LogError("[SceneInitializer] Fatal Error: GameManager missing in Build!");
#endif
    }

    // GameManager가 호출해주는 씬 초기화 진입점
    public async UniTask InitializeSceneAsync(InitializationContext context, StringBuilder log)
    {
        log.AppendLine($"[Scene] Scene Initialization Started in {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}...");

        // 0. Context 검증
        var validation = context.Validate();
        if (!validation)
        {
            if (context.MapData == null && context.MissionData != null)
                log.AppendLine("   - Warning: Mission exists but MapData is null.");

            if (!validation.IsValid && context.GlobalSettings == null)
                throw new Exception($"Scene Context Validation Failed: {validation.ErrorMessage}");
        }

        // 1. 씬 내 매니저들 초기화 순서 [수정됨]
        var initSequence = new Type[]
        {
            typeof(TileDataManager),
            typeof(MapManager),
            typeof(TilemapGenerator),
            typeof(EnvironmentManager),
            typeof(UnitManager),
            typeof(PathVisualizer),    // [추가] Unit 생성 후, 타일 시각화 도구 초기화
            typeof(PlayerController),
            typeof(CameraController),
            typeof(TurnManager),
            typeof(BattleManager),
              
        };

        foreach (var managerType in initSequence)
        {
            var managerObj = FindObjectOfType(managerType) as MonoBehaviour;
            if (managerObj != null && managerObj is IInitializable initializable)
            {
                try
                {
                    await initializable.Initialize(context);
                }
                catch (Exception ex)
                {
                    log.AppendLine($" ");
                    throw new BootstrapException($"Failed to initialize {managerType.Name}: {ex.Message}", ex);
                }
            }
        }

        // 2. 맵 비주얼 생성 (BattleScene 전용)
        var tilemapGenerator = FindObjectOfType<TilemapGenerator>();
        if (tilemapGenerator != null && context.MapData != null)
        {
            try
            {
                log.Append("      - Generating Tilemap Visuals...");
                await tilemapGenerator.GenerateAsync();
                log.AppendLine(" ");
            }
            catch (Exception ex)
            {
                log.AppendLine($"  {ex.Message}");
                throw new BootstrapException("Failed to generate tilemap visuals.", ex);
            }
        }

        log.AppendLine("   [Scene] Scene Initialization Complete.");
    }
}