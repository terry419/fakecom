using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Text; // StringBuilder 사용을 위해 추가

/// <summary>
/// 게임 시작 시 가장 먼저 실행되어, Global 스코프의 매니저들을 생성하고 초기화하는 부트스트래퍼입니다.
/// </summary>
public class AppInitializer : MonoBehaviour
{
    public static bool IsInitialized { get; private set; } = false;

    public enum BootState { None, InitializingAddressables, LoadingConfig, SpawningManagers, Initializing, Complete, Failed }
    public BootState CurrentState { get; private set; } = BootState.None;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoStart()
    {
        if (IsInitialized) return;

        var go = new GameObject("AppInitializer");
        var app = go.AddComponent<AppInitializer>();
        DontDestroyOnLoad(go);
        app.InitializeAsync().Forget();
    }
    
    public async UniTask InitializeAsync()
    {
        if (IsInitialized) return;
        IsInitialized = true;

        var sb = new StringBuilder(); // [변경] 로그를 모을 StringBuilder 생성
        sb.AppendLine("<b>[AppInitializer] 게임 시동을 겁니다...</b>");
        
        GameObject bootCanvas = null;

        try
        {
            // 단계 0: 어드레서블 시스템 초기화
            CurrentState = BootState.InitializingAddressables;
            await Addressables.InitializeAsync();
            sb.AppendLine(" - [0/5] 어드레서블 시스템 초기화 완료.");

            // 단계 1: 설정 파일(AppConfig) 로드
            CurrentState = BootState.LoadingConfig;
            var configHandle = Addressables.LoadAssetAsync<AppConfig>(ResourceKeys.AppConfig);
            AppConfig config = await configHandle;

            if (config == null)
                throw new Exception("AppConfig 파일을 찾을 수 없습니다! 어드레서블 설정을 확인하세요.");
            
            sb.AppendLine($" - [1/5] 설정 파일 로드 완료: {config.name}");

            // 단계 2: 로딩 화면 생성
            if (config.BootCanvasRef != null && config.BootCanvasRef.RuntimeKeyIsValid())
            {
                var canvasHandle = config.BootCanvasRef.InstantiateAsync();
                bootCanvas = await canvasHandle;
                DontDestroyOnLoad(bootCanvas);
                sb.AppendLine(" - [2/5] 로딩 화면 준비 완료.");
            }

            if (config.GlobalSettingsRef != null && config.GlobalSettingsRef.RuntimeKeyIsValid())
            {
                // 1. 메모리에 로드
                var settingsHandle = config.GlobalSettingsRef.LoadAssetAsync();
                GlobalSettingsSO settings = await settingsHandle;

                if (settings != null)
                {
                    // 2. ServiceLocator에 직접 등록
                    ServiceLocator.Register(settings, ManagerScope.Global);
                    sb.AppendLine($" - [3/5] GlobalSettingsSO 로드 및 등록 완료.");
                }
                else
                {
                    throw new Exception("GlobalSettingsSO 파일이 비어있거나 로드할 수 없습니다.");
                }
            }
            else
            {
                sb.AppendLine(" - <color=yellow>[주의] GlobalSettingsRef가 설정되지 않았습니다.</color>");
            }


            // 단계 3: 전역 매니저 생성
            CurrentState = BootState.SpawningManagers;
            sb.AppendLine(" - [4/5] 전역 매니저 생성 시작...");
            Transform root = new GameObject("GlobalManagers").transform;
            DontDestroyOnLoad(root.gameObject);
            
            await SpawnManager(config.InputManagerRef, root, sb);
            await SpawnManager(config.DataManagerRef, root, sb);
            await SpawnManager(config.SaveManagerRef, root, sb);
            await SpawnManager(config.GameManagerRef, root, sb);

            // 단계 4: 전역 매니저 초기화
            CurrentState = BootState.Initializing;
            sb.AppendLine(" - [5/5] 전역 매니저 초기화 시작...");
            var managers = ServiceLocator.GetByInterface<IInitializable>();
            var context = new InitializationContext
            {
                Scope = ManagerScope.Global,
                GlobalSettings = ServiceLocator.Get<GlobalSettingsSO>()
            };

            foreach (var manager in managers)
            {
                // [개선] 초기화 실패 시 어떤 매니저에서 실패했는지 알 수 있도록 try-catch 추가
                try
                {
                    await manager.Initialize(context);
                    sb.AppendLine($"   - [초기화 완료] {manager.GetType().Name}");
                }
                catch (Exception ex)
                {
                    // 더 구체적인 에러를 생성하여 throw
                    throw new Exception($"'{manager.GetType().Name}' 초기화 중 오류 발생!", ex);
                }
            }

            // 단계 5: 완료
            CurrentState = BootState.Complete;
            sb.AppendLine("\n<color=green><b>모든 시스템 준비 완료!</b></color>");
            Debug.Log(sb.ToString()); // [변경] 모아둔 로그를 한 번에 출력

            if (bootCanvas != null)
            {
                Destroy(bootCanvas);
            }
        }
        catch (Exception ex)
        {
            var failedState = CurrentState; // [변경] 실패 직전 상태 저장
            CurrentState = BootState.Failed;
            
            // [변경] 실패 지점과 원인을 명확히 로그로 남김
            Debug.LogError($"[AppInitializer] 치명적인 오류 발생! 시동 실패.\n" +
                           $" - 실패 지점: {failedState}\n" +
                           $" - 오류 내용: {ex}");
            
            // 여기에 실패 UI를 띄우는 로직 추가 가능
        }
    }

    private async UniTask SpawnManager(AssetReferenceGameObject refObj, Transform parent, StringBuilder sb)
    {
        if (refObj == null || !refObj.RuntimeKeyIsValid()) return;

        var handle = refObj.InstantiateAsync(parent);
        GameObject obj = await handle;

        if (obj != null)
        {
            obj.name = obj.name.Replace("(Clone)", "");
            sb.AppendLine($"   - [생성] {obj.name}");
        }
        else
        {
            // [개선] 생성 실패 시 원인이 되는 에셋 정보를 포함하여 에러 로그 강화
            string assetKey = refObj.AssetGUID;
            sb.AppendLine($"   - <color=red>[생성 실패]</color> 에셋 참조({{assetKey}})를 인스턴스화 할 수 없습니다.");
            // 혹은 여기서 바로 Exception을 throw하여 부팅을 중단시킬 수도 있습니다.
            // throw new Exception($"Failed to instantiate asset with GUID {assetKey}");
        }
    }
}
