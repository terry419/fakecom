using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Linq;
using System.Diagnostics; // [복구] Stopwatch용 네임스페이스

public static class AppInitializer
{
    private static bool _isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeGame()
    {
        // 1. 중복 실행 방지
        if (_isInitialized) return;
        _isInitialized = true;

        // [복구] 성능 측정 시작
        var sw = Stopwatch.StartNew();
        UnityEngine.Debug.Log("[AppInitializer] System Initialization Started...");

        try
        {
            // -----------------------------------------------------------------
            // 2. 초기화 로직 수행
            // -----------------------------------------------------------------
            var config = LoadAppConfig();

            // 어드레서블 초기화
            var initHandle = Addressables.InitializeAsync();
            initHandle.WaitForCompletion();

            if (initHandle.IsValid() && initHandle.Status != AsyncOperationStatus.Succeeded)
            {
                throw new Exception($"[FATAL] Addressables Initialization Failed. Status: {initHandle.Status}");
            }
            GameObject globalRoot = GetOrCreateGlobalRoot();

            // 데이터 및 객체 로드
            LoadGlobalSettings(config);
            SpawnInputManager(config, globalRoot.transform);

            // -----------------------------------------------------------------
            // 3. 완료 및 결과 리포트
            // -----------------------------------------------------------------
            sw.Stop();
            UnityEngine.Debug.Log($"[AppInitializer] Initialization Complete. (Time: {sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            // [복구] 실패 시에도 시간 측정 종료 및 로그 출력
            sw.Stop();
            UnityEngine.Debug.LogError($"[AppInitializer] CRITICAL ERROR! (Time: {sw.ElapsedMilliseconds}ms)\nReason: {ex}");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // --- (헬퍼 함수들은 이전과 동일하여 생략, 그대로 쓰시면 됩니다) ---
    // LoadAppConfig(), GetOrCreateGlobalRoot(), LoadGlobalSettings(), SpawnInputManager() 
    // ... 위 함수들은 아까 확정한 버전 그대로 아래에 붙여넣으시면 됩니다.

    private static AppConfig LoadAppConfig()
    {
        var config = Resources.FindObjectsOfTypeAll<AppConfig>().FirstOrDefault();
        if (config == null)
            throw new InvalidOperationException("AppConfig not found in Preloaded Assets.");
        return config;
    }

    private static GameObject GetOrCreateGlobalRoot()
    {
        var existing = GameObject.Find("GlobalManagers");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        var root = new GameObject("GlobalManagers");
        UnityEngine.Object.DontDestroyOnLoad(root);
        return root;
    }

    private static void LoadGlobalSettings(AppConfig config)
    {
        if (!config.GlobalSettingsRef.RuntimeKeyIsValid())
            throw new Exception("AppConfig: GlobalSettings reference is missing.");

        var op = config.GlobalSettingsRef.LoadAssetAsync();
        var settings = op.WaitForCompletion();

        if (op.Status != AsyncOperationStatus.Succeeded || settings == null)
            throw new Exception($"Failed to load GlobalSettingsSO from key '{config.GlobalSettingsRef.AssetGUID}'. Check Addressable Groups.");

        ServiceLocator.Register(settings);
    }

    private static void SpawnInputManager(AppConfig config, Transform parent)
    {
        if (!config.InputManagerRef.RuntimeKeyIsValid())
            throw new Exception("AppConfig: InputManager reference is missing.");

        var op = config.InputManagerRef.InstantiateAsync(parent);
        var inputObj = op.WaitForCompletion();

        if (op.Status != AsyncOperationStatus.Succeeded || inputObj == null)
            throw new Exception($"Failed to instantiate InputManager from key '{config.InputManagerRef.AssetGUID}'.");

        inputObj.name = "InputManager";

        var inputMgr = inputObj.GetComponent<InputManager>();
        if (inputMgr == null)
            throw new MissingComponentException("Prefab loaded but missing 'InputManager' component.");

        ServiceLocator.Register(inputMgr);
    }
}