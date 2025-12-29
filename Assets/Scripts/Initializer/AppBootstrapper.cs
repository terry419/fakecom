using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System;
using System.Text; // StringBuilder 사용

public static class AppBootstrapper
{
    private static bool _isGlobalInitialized = false;

    public static async UniTask EnsureGlobalSystems()
    {
        if (_isGlobalInitialized || ServiceLocator.IsRegistered<GameManager>())
        {
            _isGlobalInitialized = true;
            return;
        }

        // 성공 내역을 기록할 변수
        StringBuilder globalLog = new StringBuilder();
        globalLog.AppendLine("[Global Systems Initialization Log]");

        // 1. AppConfig 로드
        AppConfig config;
        try
        {
            config = await Addressables.LoadAssetAsync<AppConfig>("AppConfig").ToUniTask();
            globalLog.AppendLine("- AppConfig Load OK");
        }
        catch (Exception ex)
        {
            throw new Exception($"[AppBootstrapper] AppConfig 로드 실패: {ex.Message}");
        }

        if (config == null) throw new NullReferenceException("AppConfig Asset is Null");

        // 2. GlobalSettings 로드
        GlobalSettingsSO globalSettings = null;
        if (config.GlobalSettingsRef != null && config.GlobalSettingsRef.RuntimeKeyIsValid())
        {
            try
            {
                globalSettings = await config.GlobalSettingsRef.LoadAssetAsync().ToUniTask();
                globalLog.AppendLine("- GlobalSettingsSO Load OK");
            }
            catch (Exception ex)
            {
                throw new Exception($"GlobalSettingsSO 로드 실패: {ex.Message}");
            }
        }

        var context = new InitializationContext
        {
            GlobalSettings = globalSettings,
            Scope = ManagerScope.Global
        };

        // 3. 부모 오브젝트 생성
        GameObject rootParams = new GameObject("GlobalSystems");
        GameObject.DontDestroyOnLoad(rootParams);

        // 4. 매니저 생성 및 로그 기록 (logBuilder 전달)
        // 에러 발생 시 여기서 Exception이 던져지므로 catch로 넘어감
        await SpawnAndInit(config.GameManagerRef, "GameManager", context, rootParams.transform, globalLog);
        await SpawnAndInit(config.InputManagerRef, "InputManager", context, rootParams.transform, globalLog);
        await SpawnAndInit(config.SaveManagerRef, "SaveManager", context, rootParams.transform, globalLog);
        await SpawnAndInit(config.DataManagerRef, "DataManager", context, rootParams.transform, globalLog);
        await SpawnAndInit(config.EdgeDataManagerRef, "EdgeDataManager", context, rootParams.transform, globalLog);
        await SpawnAndInit(config.TileDataManagerRef, "TileDataManager", context, rootParams.transform, globalLog);

        // 여기까지 왔다면 모두 성공
        Debug.Log(globalLog.ToString()); // 전체 성공 내역 출력
        _isGlobalInitialized = true;
    }

    private static async UniTask SpawnAndInit(AssetReferenceGameObject refObj, string managerName, InitializationContext context, Transform parent, StringBuilder logBuilder)
    {
        if (refObj == null || !refObj.RuntimeKeyIsValid())
            throw new Exception($"[AppBootstrapper] '{managerName}' Reference Missing/Invalid");

        GameObject obj = null;
        try
        {
            obj = await refObj.InstantiateAsync(parent).ToUniTask();
        }
        catch (Exception ex)
        {
            throw new Exception($"[Spawn Failed] {managerName}: {ex.Message}");
        }

        obj.name = managerName;

        if (obj.TryGetComponent(out IInitializable manager))
        {
            try
            {
                await manager.Initialize(context);
                // [성공 시 로그 추가]
                logBuilder.AppendLine($"- {managerName} Initialized OK");
            }
            catch (Exception ex)
            {
                // 실패 시 로그 추가 안 함 (어차피 Exception 던짐)
                throw new Exception($"[Init Failed] {managerName}: {ex.Message}");
            }
        }
        else
        {
            throw new Exception($"[AppBootstrapper] '{managerName}' has no IInitializable.");
        }
    }
}