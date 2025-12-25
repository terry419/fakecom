using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
using System.Linq;
using System.Diagnostics;
using System.Text;

public static class AppInitializer
{
    private static bool _isInitialized = false;
    private static StringBuilder _sbLog = new StringBuilder();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeGame()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        var sw = Stopwatch.StartNew();
        _sbLog.Clear();
        _sbLog.AppendLine("[AppInitializer] System Initialization Started...");

        try
        {
            // 1. AppConfig 로드
            var config = LoadAppConfig();
            _sbLog.AppendLine($" - [Load] AppConfig Loaded. (Global: {config.GlobalSettingsRef.AssetGUID})");

            // 2. Addressables 초기화
            var initHandle = Addressables.InitializeAsync();
            initHandle.WaitForCompletion();

            if (initHandle.IsValid() && initHandle.Status == AsyncOperationStatus.Succeeded)
            {
                _sbLog.AppendLine(" - [System] Addressables Initialized.");
            }
            else if (!initHandle.IsValid())
            {
                _sbLog.AppendLine(" - [System] Addressables Initialized (Handle Released).");
            }
            else
            {
                throw new Exception($"Addressables Failed: {initHandle.Status}");
            }

            GameObject globalRoot = GetOrCreateGlobalRoot();

            // 3. 매니저 로드 및 생성
            LoadGlobalSettings(config);
            // ▼ [수정됨] 자가 등록 패턴 적용으로 name 인자 제거
            SpawnManager(config.InputManagerRef, globalRoot.transform);
            SpawnManager(config.DataManagerRef, globalRoot.transform);
            SpawnManager(config.GameManagerRef, globalRoot.transform);

            sw.Stop();
            _sbLog.AppendLine($"[AppInitializer] Initialization Complete. (Total Time: {sw.ElapsedMilliseconds}ms)");

            UnityEngine.Debug.Log(_sbLog.ToString());
        }
        catch (Exception ex)
        {
            sw.Stop();
            _sbLog.AppendLine($"[AppInitializer] CRITICAL FAILURE (Time: {sw.ElapsedMilliseconds}ms)");
            _sbLog.AppendLine($"Reason: {ex}");

            UnityEngine.Debug.LogError(_sbLog.ToString());

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }

    // --- Helper Methods ---

    // ▼ [수정됨] 매니저 등록 로직 제거 및 name 인자 제거
    private static void SpawnManager(AssetReferenceT<GameObject> refObj, Transform parent)
    {
        if (!refObj.RuntimeKeyIsValid())
            throw new Exception($"A manager reference ({refObj.RuntimeKey}) is invalid in AppConfig.");

        var op = refObj.InstantiateAsync(parent);
        var obj = op.WaitForCompletion();

        if (op.Status == AsyncOperationStatus.Succeeded && obj != null)
        {
            obj.name = op.Result.name; // 프리팹 이름으로 설정
            _sbLog.AppendLine($"   - [Instantiated] {obj.name}");
            // [제거됨] ServiceLocator.Register() 로직
            // 이제 각 매니저의 Awake()에서 스스로 등록합니다.
        }
        else
        {
            throw new Exception($"Failed to spawn manager with key {refObj.RuntimeKey}.");
        }
    }

    private static AppConfig LoadAppConfig()
    {
        var config = Resources.FindObjectsOfTypeAll<AppConfig>().FirstOrDefault();
        if (config == null) throw new InvalidOperationException("AppConfig not found in Resources!");
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
            throw new Exception("GlobalSettings Reference is invalid.");

        var op = config.GlobalSettingsRef.LoadAssetAsync();
        var settings = op.WaitForCompletion();

        if (op.Status == AsyncOperationStatus.Succeeded && settings != null)
        {
            ServiceLocator.Register(settings);
            _sbLog.AppendLine($"   - [Register] GlobalSettingsSO (Ver: {settings.GameVersion})");
        }
        else
        {
            throw new Exception("Failed to load GlobalSettingsSO.");
        }
    }
}