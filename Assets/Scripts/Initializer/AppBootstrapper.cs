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

            // 2. [Mod] 핵심 에셋 3종 로드 (Settings, Registry, *Catalog*)
            var coreAssets = await LoadCoreAssetsAsync(config, globalLog);

            // 3. 컨텍스트 생성
            var context = new InitializationContext
            {
                GlobalSettings = coreAssets.settings,
                Registry = coreAssets.registry,
                MapCatalog = coreAssets.catalog, // [New] 카탈로그 주입
                Scope = ManagerScope.Global
            };
            globalLog.AppendLine("- Initialization Context Created: SUCCESS");

            // 4. 매니저 루트 오브젝트 생성
            GameObject rootParams = new GameObject("GlobalSystems");
            GameObject.DontDestroyOnLoad(rootParams);

            // =================================================================
            // [중요] 5. MapCatalogManager 수동 생성 (DataManager보다 먼저!)
            // =================================================================
            GameObject catalogObj = new GameObject("MapCatalogManager");
            catalogObj.transform.SetParent(rootParams.transform); // 계층 정리
            var catalogMgr = catalogObj.AddComponent<MapCatalogManager>();

            // Awake(Register) -> Initialize 순서 실행
            // Context에 이미 Catalog가 있으므로 Addressable 로드 없이 즉시 초기화됨
            await catalogMgr.Initialize(context);
            globalLog.AppendLine("- MapCatalogManager Init: OK");

            // =================================================================
            // 6. 기존 매니저 생성 및 초기화
            // =================================================================
            await SpawnAndInit(config.GameManagerRef, "GameManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.InputManagerRef, "InputManager", context, rootParams.transform, globalLog);
            await SpawnAndInit(config.SaveManagerRef, "SaveManager", context, rootParams.transform, globalLog);

            // DataManager는 MapCatalogManager에 의존하므로 이 시점에는 안전함
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
        CheckRef("TileRegistryRef", config.TileRegistryRef, missingRefs, isCritical: true);

        // [New] 카탈로그 참조 검사 추가
        CheckRef("MapCatalogRef", config.MapCatalogRef, missingRefs, isCritical: true);

        CheckRef("GameManagerRef", config.GameManagerRef, missingRefs, isCritical: true);
        CheckRef("DataManagerRef", config.DataManagerRef, missingRefs, isCritical: true);
        CheckRef("InputManagerRef", config.InputManagerRef, missingRefs, isCritical: true);
        CheckRef("SaveManagerRef", config.SaveManagerRef, missingRefs, isCritical: true);
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

    // [Mod] 리턴 타입 변경: (Settings, Registry, Catalog) 3개를 반환
    private static async UniTask<(GlobalSettingsSO settings, TileRegistrySO registry, MapCatalogSO catalog)>
        LoadCoreAssetsAsync(AppConfig config, StringBuilder globalLog)
    {
        try
        {
            // 병렬 로드
            var t1 = LoadSingleAssetAsync(config.GlobalSettingsRef, "GlobalSettingsSO");
            var t2 = LoadSingleAssetAsync(config.TileRegistryRef, "TileRegistrySO");
            var t3 = LoadSingleAssetAsync(config.MapCatalogRef, "MapCatalogSO"); // [New]

            var results = await UniTask.WhenAll(t1, t2, t3);

            if (results.Item1 == null) throw new NullReferenceException("GlobalSettingsSO is null");
            if (results.Item2 == null) throw new NullReferenceException("TileRegistrySO is null");
            if (results.Item3 == null) throw new NullReferenceException("MapCatalogSO is null");

            globalLog.AppendLine("- Core Assets (Settings, Registry, Catalog) Loaded OK");

            return (results.Item1, results.Item2, results.Item3);
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

    // (사용처가 있다면 유지, 없다면 삭제 가능하지만 요청대로 기능 보존)
    private static void ValidateCatalogAsset(MapCatalogSO catalog)
    {
        if (!catalog.ValidateAllPools(out string errorMsg))
        {
            throw new BootstrapException($"[MapCatalog] Validation Failed:\n{errorMsg}");
        }
    }
}