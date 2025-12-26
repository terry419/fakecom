using System;
using UnityEngine;
using UnityEngine.AddressableAssets; // 리소스 로드용
using UnityEngine.ResourceManagement.AsyncOperations; // 로딩 상태 확인용
using Cysharp.Threading.Tasks; // 비동기 작업용
using System.Collections.Generic;

// MonoBehaviour를 상속받아서 게임 오브젝트로 존재하게 합니다.
public class AppInitializer : MonoBehaviour
{
    // [중요] 게임이 켜졌는지 확인하는 깃발
    public static bool IsInitialized { get; private set; } = false;

    // 현재 부팅 상태 (로딩 중인지, 끝났는지)
    public enum BootState { None, LoadingConfig, SpawningManagers, Initializing, Complete, Failed }
    public BootState CurrentState { get; private set; } = BootState.None;

    // 생성된 글로벌 매니저들을 담아둘 리스트 (나중에 초기화 명령 내리려고)
    private List<IInitializable> _globalManagers = new List<IInitializable>();


    // ==================================================================================
    // 1. 엔진 시동 (유니티가 시작되면 무조건 실행됨)
    // ==================================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        // 이미 켜져있으면 또 켜지 마라
        if (IsInitialized) return;

        // "AppInitializer"라는 이름의 빈 게임 오브젝트를 만들고, 이 스크립트를 붙입니다.
        var initializerObject = new GameObject("AppInitializer");
        var initializerInstance = initializerObject.AddComponent<AppInitializer>();

        // 씬이 바뀌어도 파괴되지 않게 설정
        DontDestroyOnLoad(initializerObject);

        // 비동기 시동 시작! (결과를 기다리지 않고 바로 시작)
        initializerInstance.InitializeAsync().Forget();
    }


    // ==================================================================================
    // 2. 실제 시동 로직 (순서대로 진행)
    // ==================================================================================
    private async UniTaskVoid InitializeAsync()
    {
        Debug.Log("[AppInitializer] 게임 시동을 겁니다...");
        IsInitialized = true;
        GameObject bootCanvas = null; // 로딩 화면

        try
        {
            // -----------------------------------------------------------------
            // 단계 0: 어드레서블(리소스 시스템) 초기화
            // -----------------------------------------------------------------
            await Addressables.InitializeAsync();

            // -----------------------------------------------------------------
            // 단계 1: 설정 파일(AppConfig) 가져오기
            // -----------------------------------------------------------------
            CurrentState = BootState.LoadingConfig;

            // "주소록"에 적힌 이름으로 파일을 찾습니다.
            var configHandle = Addressables.LoadAssetAsync<AppConfig>(ResourceKeys.AppConfig);
            AppConfig config = await configHandle;

            if (config == null)
            {
                throw new Exception("AppConfig 파일을 찾을 수 없습니다! 어드레서블 설정을 확인하세요.");
            }

            // -----------------------------------------------------------------
            // 단계 2: 로딩 화면 띄우기
            // -----------------------------------------------------------------
            if (config.BootCanvasRef != null && config.BootCanvasRef.RuntimeKeyIsValid())
            {
                var canvasHandle = config.BootCanvasRef.InstantiateAsync();
                bootCanvas = await canvasHandle;
                DontDestroyOnLoad(bootCanvas); // 로딩 화면도 씬 전환 때 꺼지면 안 됨
            }

            // -----------------------------------------------------------------
            // 단계 3: 매니저들 생성하기 (쇼핑 리스트대로)
            // -----------------------------------------------------------------
            CurrentState = BootState.SpawningManagers;

            // 매니저들이 모여살 'GlobalManagers'라는 빈 부모 오브젝트를 만듭니다.
            Transform root = new GameObject("GlobalManagers").transform;
            DontDestroyOnLoad(root.gameObject);

            // 하나씩 생성합니다. (순서 중요!)
            // 생성된 매니저들은 자기들의 Awake()에서 ServiceLocator에 스스로 등록합니다.
            await SpawnManager(config.GlobalSettingsRef, root);
            await SpawnManager(config.InputManagerRef, root);
            await SpawnManager(config.DataManagerRef, root);
            await SpawnManager(config.SaveManagerRef, root);
            await SpawnManager(config.GameManagerRef, root);


            // -----------------------------------------------------------------
            // 단계 4: 매니저들 초기화 (일할 준비 시키기)
            // -----------------------------------------------------------------
            CurrentState = BootState.Initializing;

            // ServiceLocator에서 "초기화 필요한 애들(IInitializable) 다 나와!" 하고 부릅니다.
            // Global 칸에 있는 애들만 부릅니다.
            // 주의: ServiceLocator.GetByInterface는 모든 칸을 다 뒤지지만, 
            // 지금은 부팅 시점이라 Global 칸밖에 없어서 상관없습니다.
            var managers = ServiceLocator.GetByInterface<IInitializable>();

            // 보급품 가방(Context)을 쌉니다.
            var context = new InitializationContext
            {
                Scope = ManagerScope.Global,
                // GlobalSettings는 이미 로드되어서 ServiceLocator에 등록되어 있을 겁니다.
                GlobalSettings = ServiceLocator.Get<GlobalSettingsSO>()
            };

            // 모든 매니저에게 "준비해!" 명령을 내리고 다 될 때까지 기다립니다.
            foreach (var manager in managers)
            {
                await manager.Initialize(context);
                Debug.Log($" - [완료] {manager.GetType().Name} 초기화됨.");
            }


            // -----------------------------------------------------------------
            // 단계 5: 완료!
            // -----------------------------------------------------------------
            CurrentState = BootState.Complete;
            Debug.Log("<color=green>[AppInitializer] 모든 시스템 준비 완료!</color>");

            // 로딩 화면이 있었다면 이제 끕니다.
            if (bootCanvas != null)
            {
                Destroy(bootCanvas);
            }
        }
        catch (Exception ex)
        {
            CurrentState = BootState.Failed;
            Debug.LogError($"[AppInitializer] 치명적인 오류 발생! 시동 실패: {ex}");
            // 실패했을 때 로딩 화면에 "에러 발생"이라고 띄우는 처리가 나중에 필요할 수 있습니다.
        }
    }


    // [도우미] 어드레서블로 매니저 하나 생성하는 함수
    private async UniTask SpawnManager(AssetReferenceGameObject refObj, Transform parent)
    {
        // 리스트에 없거나 잘못된 거면 패스
        if (refObj == null || !refObj.RuntimeKeyIsValid()) return;

        // 생성 (비동기)
        var handle = refObj.InstantiateAsync(parent);
        GameObject obj = await handle;

        if (obj != null)
        {
            // 이름 뒤에 (Clone) 붙는 거 보기 싫어서 뗍니다.
            obj.name = obj.name.Replace("(Clone)", "");
            Debug.Log($" - [생성] {obj.name}");
        }
    }
}