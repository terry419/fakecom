using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System;

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

        // 1. AppConfig 로드 시도
        AppConfig config;
        try
        {
            config = await Addressables.LoadAssetAsync<AppConfig>("AppConfig").ToUniTask();
        }
        catch (Exception ex)
        {
            throw new Exception($"[AppBootstrapper] 'AppConfig' 로드 실패! Addressables 빌드를 확인하세요. (Error: {ex.Message})");
        }

        if (config == null)
            throw new NullReferenceException("[AppBootstrapper] 'AppConfig' 에셋을 찾았으나 내용이 비어있습니다(Null).");

        // 2. 각 매니저 생성 (이름을 달아서 추적 가능하게 함)
        var context = new InitializationContext { GlobalSettings = null };

        await SpawnAndInit(config.GameManagerRef, "GameManager", context);
        await SpawnAndInit(config.InputManagerRef, "InputManager", context);
        await SpawnAndInit(config.SaveManagerRef, "SaveManager", context);
        await SpawnAndInit(config.DataManagerRef, "DataManager", context);
        await SpawnAndInit(config.EdgeDataManagerRef, "EdgeDataManager", context);
        await SpawnAndInit(config.TileDataManagerRef, "TileDataManager", context);

        _isGlobalInitialized = true;
    }

    private static async UniTask SpawnAndInit(AssetReferenceGameObject refObj, string managerName, InitializationContext context)
    {
        // [범인 색출 1] AppConfig에 슬롯이 비어있는 경우
        if (refObj == null)
        {
            Debug.LogError($"<color=red>[AppBootstrapper] '{managerName}'의 프리팹이 AppConfig에 연결되지 않았습니다!</color>");
            return; // 에러 로그 띄우고 패스 (일단 게임은 켜지게)
        }

        // [범인 색출 2] Addressables 키가 깨진 경우
        if (!refObj.RuntimeKeyIsValid())
        {
            Debug.LogError($"<color=red>[AppBootstrapper] '{managerName}'의 Addressable Key가 유효하지 않습니다. (Rebuild 필요)</color>");
            return;
        }

        GameObject obj = null;
        try
        {
            obj = await refObj.InstantiateAsync().ToUniTask();
        }
        catch (Exception ex)
        {
            // [범인 색출 3] 생성 도중 에러 (프리팹 내부 문제 등)
            throw new Exception($"[AppBootstrapper] '{managerName}' 프리팹 생성(Instantiate) 중 오류 발생! 프리팹을 확인하세요.\n{ex.Message}");
        }

        // [범인 색출 4] 생성은 했는데 결과가 Null인 경우 (매우 드묾)
        if (obj == null)
        {
            throw new Exception($"[AppBootstrapper] '{managerName}' 생성 결과가 NULL입니다.");
        }

        obj.name = managerName; // 이름 깔끔하게 정리
        GameObject.DontDestroyOnLoad(obj);

        if (obj.TryGetComponent(out IInitializable manager))
        {
            await manager.Initialize(context);
        }
        else
        {
            Debug.LogError($"[AppBootstrapper] '{managerName}' 프리팹에 'IInitializable' 스크립트가 없습니다!");
        }
    }
}