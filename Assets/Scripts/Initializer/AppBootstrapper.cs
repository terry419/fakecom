using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System;
using System.Text;
using System.Collections.Generic;

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

        StringBuilder globalLog = new StringBuilder();
        globalLog.AppendLine("[Global Systems Initialization Log]");

        try
        {
            // 1. AppConfig 로드
            AppConfig config = await LoadAndValidateAppConfig(globalLog);

            // 2. [Mod] 핵심 에셋 로드 (VisualSettings -> Registry)
            var (globalSettings, registry) = await LoadCoreAssetsAsync(config, globalLog);

            // 3. 컨텍스트 생성
            var context = new InitializationContext
            {
                GlobalSettings = globalSettings,
                Registry = registry, // [Mod] 교체된 레지스트리 주입
                Scope = ManagerScope.Global
            };
            globalLog.AppendLine("- Initialization Context Created: SUCCESS");

            // 4. 매니저 루트 생성
            GameObject rootParams = new GameObject("GlobalSystems");
            GameObject.DontDestroyOnLoad(rootParams);

            // 5. 매니저 생성 및 초기화 (기존 유지)
            await SpawnAndInit(config.GameManagerRef, "GameManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.InputManagerRef, "InputManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.SaveManagerRef, "SaveManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.DataManagerRef, "DataManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.TileDataManagerRef, "TileDataManager", context, rootParams.transform, globalLog);

            globalLog.AppendLine("\nALL GLOBAL SYSTEMS INITIALIZED SUCCESSFULLY");
            Debug.Log(globalLog.ToString());

            _isGlobalInitialized = true;
        }
        catch (Exception ex)
        {
            throw new BootstrapException($"[AppBootstrapper] Unexpected error: {ex.GetType().Name}", ex);
        }
    }

    // ... (LoadAndValidateAppConfig는 유지) ...
    private static async UniTask<AppConfig> LoadAndValidateAppConfig(StringBuilder globalLog)
    {
        AppConfig config;
        try
        {
            config = await Addressables.LoadAssetAsync<AppConfig>("AppConfig").ToUniTask();
            if (config == null) throw new NullReferenceException("AppConfig loaded but null");
            globalLog.AppendLine("- AppConfig Load: OK");
        }
        catch (Exception ex)
        {
            throw new BootstrapException($"[AppBootstrapper] Failed to load AppConfig: {ex.Message}", ex);
        }

        ValidateAppConfigReferences(config);
        return config;
    }

    private static void ValidateAppConfigReferences(AppConfig config)
    {
        var missingRefs = new List<string>();

        CheckRef("GlobalSettingsRef", config.GlobalSettingsRef, missingRefs, isCritical: true);

        // [Mod] 참조 검사 대상 변경
        CheckRef("TileRegistryRef", config.TileRegistryRef, missingRefs, isCritical: true);

        CheckRef("GameManagerRef", config.GameManagerRef, missingRefs, isCritical: true);
        // ... (나머지 매니저 검사 유지) ...
        CheckRef("TileDataManagerRef", config.TileDataManagerRef, missingRefs, isCritical: true);

        if (missingRefs.Count > 0)
        {
            string missing = string.Join("\n - ", missingRefs);
            throw new BootstrapException($"[AppBootstrapper] Missing References:\n - {missing}");
        }
    }

    private static void CheckRef<T>(string name, AssetReferenceT<T> refObj, List<string> list, bool isCritical) where T : UnityEngine.Object
    {
        if (refObj == null || !refObj.RuntimeKeyIsValid()) list.Add(name);
    }

    // [Mod] 리턴 타입 및 로드 대상 변경: MapEditorSettingsSO -> TileRegistrySO
    private static async UniTask<(GlobalSettingsSO, TileRegistrySO)> LoadCoreAssetsAsync(AppConfig config, StringBuilder globalLog)
    {
        try
        {
            // 병렬 로드 유지
            var globalTask = LoadSingleAssetAsync(config.GlobalSettingsRef, "GlobalSettingsSO");
            var registryTask = LoadSingleAssetAsync(config.TileRegistryRef, "TileRegistrySO");

            var (globalSettings, registry) = await UniTask.WhenAll(globalTask, registryTask);

            if (globalSettings == null) throw new NullReferenceException("GlobalSettingsSO is null");
            if (registry == null) throw new NullReferenceException("TileRegistrySO is null");

            globalLog.AppendLine("- Core Assets (GlobalSettings, TileRegistry) Loaded OK");

            return (globalSettings, registry);
        }
        catch (Exception ex)
        {
            throw new BootstrapException($"[AppBootstrapper] Failed to load core assets: {ex.Message}", ex);
        }
    }

    private static async UniTask<T> LoadSingleAssetAsync<T>(AssetReferenceT<T> assetRef, string assetName) where T : UnityEngine.Object
    {
        var result = await assetRef.LoadAssetAsync().ToUniTask();
        if (result == null) throw new NullReferenceException($"{assetName} is null");
        return result;
    }

    // ... (SpawnAndInit 유지) ...
    private static async UniTask SpawnAndInit(
        AssetReferenceGameObject refObj,
        string managerName,
        InitializationContext context,
        Transform parent,
        StringBuilder logBuilder)
    {
        var obj = await refObj.InstantiateAsync(parent).ToUniTask();
        obj.name = managerName;

        if (obj.TryGetComponent(out IInitializable manager))
        {
            await manager.Initialize(context);
            logBuilder.AppendLine($"- {managerName} Init: OK");
        }
    }

    private static void ValidateCatalogAsset(MapCatalogSO catalog)
    {
        // [개선 8] 부팅 시점에 데이터 무결성 검증
        if (!catalog.ValidateAllPools(out string errorMsg))
        {
            // 데이터 오류는 치명적이므로 부팅 중단
            throw new BootstrapException($"[MapCatalog] Validation Failed:\n{errorMsg}");
        }
    }
}