using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;

public class SceneInitializer : MonoBehaviour
{
    [Header("Editor Test Config")]
    [Tooltip("에디터에서 바로 시작할 때 사용할 테스트용 미션 데이터")]
    [SerializeField] private MissionDataSO _testMissionData;

    // Start는 유니티가 자동으로 호출합니다.
    private async void Start()
    {
        // 1. [정상 루트] BootManager나 GameManager를 통해 이미 시스템이 로드된 상태인지 확인
        // ServiceLocator에 GameManager가 있다는 뜻은 이미 Boot 과정을 거쳤다는 의미입니다.
        if (ServiceLocator.IsRegistered<GameManager>())
        {
            // GameManager가 곧 InitializeSceneAsync를 호출해 줄 것이므로,
            // 여기서는 아무것도 하지 않고 기다립니다 (중복 실행 방지).
            return;
        }

        // 2. [테스트 루트] GameManager가 없다면, 개발자가 이 씬에서 바로 Play를 누른 것입니다.
        // 직접 초기화 로직을 돌리는 대신, 'GameManager'를 깨워서 정식 세션 시작을 요청합니다.

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

            // 만약 테스트 데이터도 없으면 (BaseScene 등) 그냥 빈 세션 시작
            // (BaseScene 테스트라면 missionToRun이 null이어도 상관없음)

            // D. [핵심] GameManager에게 "이 미션으로 세션을 시작해줘"라고 요청
            // GameManager는 내부적으로 씬을 다시 로드(Reload)하거나 준비한 뒤,
            // 정상적인 경로로 다시 이 스크립트의 InitializeSceneAsync를 호출하게 됩니다.
            // * ownsMission: false (에디터 에셋이므로 파괴 방지)
            await gameManager.StartSessionAsync(missionToRun, false);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneInitializer] Self-Boot Error: {ex.Message}\n{ex.StackTrace}");
        }
#else
        // 빌드된 게임에서 GameManager 없이 씬이 켜졌다면 치명적 오류입니다.
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
            // Scene Scope에서는 엄격하게 검사 (BaseScene 등 맵이 없는 씬은 Context 구성 시 MapData를 null로 넘기면 됨)
            // 단, BattleScene인데 MapData가 없다면 에러여야 함.
            if (context.MapData == null && context.MissionData != null)
                log.AppendLine("   - Warning: Mission exists but MapData is null.");

            // 필수 매니저가 없는 경우만 에러 처리
            if (!validation.IsValid && context.GlobalSettings == null)
                throw new Exception($"Scene Context Validation Failed: {validation.ErrorMessage}");
        }

        // 1. 씬 내 매니저들 초기화 순서
        // (BaseScene이라면 TilemapGenerator 등이 없을 수 있으므로 FindObjectOfType으로 안전하게 처리됨)
        var initSequence = new Type[]
        {
            typeof(TileDataManager),
            typeof(MapManager),
            typeof(TilemapGenerator),
            typeof(EnvironmentManager),
            typeof(UnitManager),
            typeof(TurnManager),
            typeof(CameraController), // 카메라
            // 필요한 경우 BaseScene 전용 매니저(MainMenuController 등)도 추가 가능
        };

        foreach (var managerType in initSequence)
        {
            var managerObj = FindObjectOfType(managerType) as MonoBehaviour;
            if (managerObj != null && managerObj is IInitializable initializable)
            {
                try
                {
                    log.Append($"     - {managerType.Name}...");
                    await initializable.Initialize(context);
                    log.AppendLine(" ");
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
                log.Append("     - Generating Tilemap Visuals...");
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