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

            // 3. 매니저 로드 및 등록 (순서 중요)
            LoadGlobalSettings(config);
            SpawnManager(config.InputManagerRef, "InputManager", globalRoot.transform);

            // [추가됨] DataManager와 GameManager 생성
            SpawnManager(config.DataManagerRef, "DataManager", globalRoot.transform);
            SpawnManager(config.GameManagerRef, "GameManager", globalRoot.transform);

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

    // [리팩토링] 매니저 생성 코드가 중복되므로 함수 하나로 통합했습니다.
    private static void SpawnManager(AssetReferenceT<GameObject> refObj, string name, Transform parent)
    {
        if (!refObj.RuntimeKeyIsValid())
            throw new Exception($"{name} Reference is invalid in AppConfig.");

        var op = refObj.InstantiateAsync(parent);
        var obj = op.WaitForCompletion();

        if (op.Status == AsyncOperationStatus.Succeeded && obj != null)
        {
            obj.name = name;

            // 컴포넌트를 찾아서 ServiceLocator에 등록
            // (모든 매니저는 MonoBehaviour를 상속받으므로 Component로 찾음)
            var component = obj.GetComponent<MonoBehaviour>();
            if (component != null)
            {
                // 제네릭 메서드 호출을 위해 리플렉션 대신 dynamic이나 인터페이스 사용 고려 가능하나
                // 여기서는 ServiceLocator.Register(object) 오버로딩이 있다면 편함.
                // 현재 ServiceLocator는 제네릭 <T>만 지원하므로, 구체적인 타입을 알기 위해선
                // 각 매니저별로 코드를 분리하거나, ServiceLocator에 Register(Type, object)를 추가해야 함.

                // [중요] 현재 구조상 가장 깔끔한 방법:
                if (name == "InputManager") ServiceLocator.Register(obj.GetComponent<InputManager>());
                else if (name == "DataManager") ServiceLocator.Register(obj.GetComponent<DataManager>());
                else if (name == "GameManager") ServiceLocator.Register(obj.GetComponent<GameManager>());

                _sbLog.AppendLine($"   - [Register] {name}");
            }
            else
            {
                _sbLog.AppendLine($"   - [Warning] {name} spawned but no MonoBehaviour found.");
            }
        }
        else
        {
            throw new Exception($"Failed to spawn {name}.");
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