using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

public static class AppInitializer
{
    // 중복 실행 방지 플래그
    private static bool _isInitialized = false;

    // 게임 시작 전(Scene 로드 전) 가장 먼저 실행되는 진입점
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeGame()
    {
        if (_isInitialized) return;
        _isInitialized = true;

        // 로딩 시간 측정을 위한 스톱워치 (UnityEngine.Debug와 충돌 방지 위해 풀네임 사용)
        System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
        Debug.Log("[AppInitializer] System Initialization Started...");

        try
        {
            // -----------------------------------------------------------------
            // 1. Addressables 시스템 초기화
            // -----------------------------------------------------------------
            Debug.Log("[AppInitializer] Step 1: Initializing Addressables...");

            var initHandle = Addressables.InitializeAsync();
            initHandle.WaitForCompletion(); // 동기 대기

            // 핸들 유효성 및 상태 검사
            if (!initHandle.IsValid())
                throw new Exception("Addressables Init Handle is invalid.");

            if (initHandle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Addressables Init Failed. Status: {initHandle.Status}");

            Debug.Log("[AppInitializer] Step 1 Complete.");

            // -----------------------------------------------------------------
            // 2. AppConfig 로드 ("AppConfig"라는 주소로 로드)
            // -----------------------------------------------------------------
            Debug.Log("[AppInitializer] Step 2: Loading AppConfig...");

            var configHandle = Addressables.LoadAssetAsync<AppConfig>("AppConfig");
            var config = configHandle.WaitForCompletion();

            // 로드 실패 시 예외 처리
            if (!configHandle.IsValid())
                throw new Exception("AppConfig Handle is invalid.");

            if (configHandle.Status != AsyncOperationStatus.Succeeded || config == null)
                throw new Exception($"Failed to load AppConfig. Status: {configHandle.Status}");

            Debug.Log("[AppInitializer] Step 2 Complete.");

            // -----------------------------------------------------------------
            // 3. 매니저 생성 및 설정 (GlobalManagers 루트 아래 생성)
            // -----------------------------------------------------------------
            GameObject globalRoot = GetOrCreateGlobalRoot();

            // 3-1. GlobalSettingsSO 로드 및 등록
            // 사용자님이 주신 AppConfig의 Property(Getter)를 사용함
            LoadGlobalSettings(config);

            // 3-2. 매니저 프리팹 생성 (자가 등록 방식을 사용하므로 Register 호출 안 함)
            // 사용자님이 주신 AppConfig의 Property(Getter)를 사용함
            SpawnManager(config.InputManagerRef, "InputManager", globalRoot.transform);
            SpawnManager(config.DataManagerRef, "DataManager", globalRoot.transform);
            SpawnManager(config.GameManagerRef, "GameManager", globalRoot.transform);

            // 초기화 완료 로그
            sw.Stop();
            Debug.Log($"[AppInitializer] Initialization Complete. (Total Time: {sw.ElapsedMilliseconds}ms)");
        }
        catch (Exception ex)
        {
            // 치명적 오류 발생 시 게임 정지
            sw.Stop();
            Debug.LogError($"[AppInitializer] CRITICAL FAILURE: {ex}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }

    // [Helper] 전역 매니저들을 담을 부모 오브젝트 생성 (DontDestroyOnLoad)
    private static GameObject GetOrCreateGlobalRoot()
    {
        var existing = GameObject.Find("GlobalManagers");
        if (existing != null) UnityEngine.Object.DestroyImmediate(existing);

        var root = new GameObject("GlobalManagers");
        UnityEngine.Object.DontDestroyOnLoad(root);
        return root;
    }

    // [Helper] 매니저 생성 함수
    private static void SpawnManager(AssetReferenceT<GameObject> refObj, string name, Transform parent)
    {
        // Reference가 비어있는지 확인
        if (!refObj.RuntimeKeyIsValid())
            throw new Exception($"{name} reference key is invalid in AppConfig.");

        // 프리팹 인스턴스화
        var op = refObj.InstantiateAsync(parent);
        var obj = op.WaitForCompletion();

        // 생성 성공 여부 확인
        if (op.Status == AsyncOperationStatus.Succeeded && obj != null)
        {
            obj.name = name; // 이름 정리
            Debug.Log($" - [Spawned] {name}");
        }
        else
        {
            throw new Exception($"Failed to spawn {name}.");
        }
    }

    // [Helper] GlobalSettingsSO 로드 및 등록 함수
    private static void LoadGlobalSettings(AppConfig config)
    {
        // AppConfig의 GlobalSettingsRef 프로퍼티 사용
        if (!config.GlobalSettingsRef.RuntimeKeyIsValid())
            throw new Exception("GlobalSettings reference key is invalid.");

        var op = config.GlobalSettingsRef.LoadAssetAsync();
        var settings = op.WaitForCompletion();

        if (op.Status == AsyncOperationStatus.Succeeded && settings != null)
        {
            // ScriptableObject는 Awake가 없으므로 여기서 수동 등록 필수
            ServiceLocator.Register(settings);
            Debug.Log(" - [Registered] GlobalSettingsSO");
        }
        else
        {
            throw new Exception("Failed to load GlobalSettingsSO.");
        }
    }
}