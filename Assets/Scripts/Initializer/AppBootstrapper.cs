using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;

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
            // 1. AppConfig 로드 및 유효성 검사
            AppConfig config = await LoadAndValidateAppConfig(globalLog);

            // 2. 핵심 에셋 병렬 로드
            var (globalSettings, visualSettings) = await LoadCoreAssetsAsync(config, globalLog);

            // 3. 컨텍스트 생성
            var context = new InitializationContext
            {
                GlobalSettings = globalSettings,
                MapVisualSettings = visualSettings,
                Scope = ManagerScope.Global
            };
            globalLog.AppendLine("- Initialization Context Created: SUCCESS");

            // 4. 매니저 루트 생성
            GameObject rootParams = new GameObject("GlobalSystems");
            GameObject.DontDestroyOnLoad(rootParams);

            // 5. 매니저 생성 및 초기화
            await SpawnAndInit(config.GameManagerRef, "GameManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.InputManagerRef, "InputManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.SaveManagerRef, "SaveManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.DataManagerRef, "DataManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.EdgeDataManagerRef, "EdgeDataManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.TileDataManagerRef, "TileDataManager", context, rootParams.transform, globalLog);

            globalLog.AppendLine("\nALL GLOBAL SYSTEMS INITIALIZED SUCCESSFULLY");
            Debug.Log(globalLog.ToString());

            _isGlobalInitialized = true;
        }
        catch (BootstrapException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new BootstrapException($"[AppBootstrapper] Unexpected error: {ex.GetType().Name}", ex);
        }
    }

    private static async UniTask<AppConfig> LoadAndValidateAppConfig(StringBuilder globalLog)
    {
        AppConfig config;
        try
        {
            config = await Addressables.LoadAssetAsync<AppConfig>("AppConfig").ToUniTask();
            if (config == null)
                throw new NullReferenceException("AppConfig asset loaded but is null");

            globalLog.AppendLine("- AppConfig Load: OK");
        }
        catch (Exception ex)
        {
            throw new BootstrapException(
                $"[AppBootstrapper] Failed to load AppConfig:\n{ex.Message}\n\n" +
                $"Troubleshooting:\n" +
                $"  1. Verify 'AppConfig' asset exists and is marked Addressable\n" +
                $"  2. Check Addressables Groups for 'AppConfig' label/address", ex);
        }

        ValidateAppConfigReferences(config);
        globalLog.AppendLine("- AppConfig References Validation: OK");

        return config;
    }

    private static void ValidateAppConfigReferences(AppConfig config)
    {
        var missingRefs = new List<string>();

        CheckRef("GlobalSettingsRef", config.GlobalSettingsRef, missingRefs, isCritical: true);
        CheckRef("MapVisualSettingsRef", config.MapVisualSettingsRef, missingRefs, isCritical: true);
        CheckRef("GameManagerRef", config.GameManagerRef, missingRefs, isCritical: true);
        CheckRef("InputManagerRef", config.InputManagerRef, missingRefs, isCritical: true);
        CheckRef("SaveManagerRef", config.SaveManagerRef, missingRefs, isCritical: true);
        CheckRef("DataManagerRef", config.DataManagerRef, missingRefs, isCritical: true);
        CheckRef("EdgeDataManagerRef", config.EdgeDataManagerRef, missingRefs, isCritical: true);
        CheckRef("TileDataManagerRef", config.TileDataManagerRef, missingRefs, isCritical: true);

        if (missingRefs.Count > 0)
        {
            string missing = string.Join("\n  - ", missingRefs);
            throw new BootstrapException(
                $"[AppBootstrapper] CRITICAL: AppConfig has missing/invalid Addressable references:\n" +
                $"  - {missing}\n\n" +
                $"Please ensure:\n" +
                $"  1. All references are assigned in AppConfig Inspector\n" +
                $"  2. All target assets are marked as 'Addressable'\n" +
                $"  3. Rebuild Addressables index if modified recently");
        }
    }

    private static void CheckRef<T>(
        string fieldName,
        AssetReferenceT<T> assetRef,
        List<string> missingList,
        bool isCritical = false) where T : UnityEngine.Object
    {
        bool isInvalid = assetRef == null || !assetRef.RuntimeKeyIsValid();
        if (isInvalid)
        {
            string prefix = isCritical ? "[CRITICAL] " : "[MISSING] ";
            missingList.Add($"{prefix}{fieldName}");
        }
    }

    private static async UniTask<(GlobalSettingsSO, MapEditorSettingsSO)> LoadCoreAssetsAsync(AppConfig config, StringBuilder globalLog)
    {
        try
        {
            // 1. 태스크 생성 (여기서는 await 하지 않음)
            var globalTask = LoadSingleAssetAsync(config.GlobalSettingsRef, "GlobalSettingsSO");
            var visualTask = LoadSingleAssetAsync(config.MapVisualSettingsRef, "MapEditorSettingsSO");

            // 2. [수정됨] WhenAll 결과를 바로 튜플로 받습니다. (개별 await 금지!)
            var (globalSettings, visualSettings) = await UniTask.WhenAll(globalTask, visualTask);

            // 3. 결과 검증
            if (globalSettings == null) throw new NullReferenceException("GlobalSettingsSO is null");
            if (visualSettings == null) throw new NullReferenceException("MapEditorSettingsSO is null");

            globalLog.AppendLine("- Core Assets (GlobalSettings, MapVisualSettings) Loaded OK");

            return (globalSettings, visualSettings);
        }
        catch (BootstrapException) { throw; }
        catch (Exception ex)
        {
            throw new BootstrapException($"[AppBootstrapper] Failed to load core assets: {ex.Message}", ex);
        }
    }
    private static async UniTask<T> LoadSingleAssetAsync<T>(AssetReferenceT<T> assetRef, string assetName) where T : UnityEngine.Object
    {
        try
        {
            var result = await assetRef.LoadAssetAsync().ToUniTask();
            if (result == null) throw new NullReferenceException($"{assetName} loaded asset is null");
            return result;
        }
        catch (Exception ex)
        {
            throw new BootstrapException($"Failed to load {assetName}: {ex.Message}", ex);
        }
    }

    private static async UniTask SpawnAndInit(
        AssetReferenceGameObject refObj,
        string managerName,
        InitializationContext context,
        Transform parent,
        StringBuilder logBuilder)
    {
        GameObject obj = null;
        try
        {
            obj = await refObj.InstantiateAsync(parent).ToUniTask();
            if (obj == null) throw new NullReferenceException("Instantiate returned null");
        }
        catch (Exception ex)
        {
            throw new BootstrapException(
                $"[SPAWN FAILED] {managerName}: Instantiation failed.\n" +
                $"Error: {ex.Message}", ex);
        }

        obj.name = managerName;

        if (!obj.TryGetComponent(out IInitializable manager))
        {
            throw new BootstrapException(
                $"[MISSING COMPONENT] '{managerName}' prefab is missing IInitializable.");
        }

        try
        {
            await manager.Initialize(context);
            logBuilder.AppendLine($"- {managerName} Initialization: OK");
        }
        catch (Exception ex)
        {
            throw new BootstrapException(
                $"[INIT FAILED] {managerName}.Initialize() error.\n" +
                $"Error: {ex.Message}", ex);
        }
    }
}