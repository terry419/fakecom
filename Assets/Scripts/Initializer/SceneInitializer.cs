using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;

public class SceneInitializer : MonoBehaviour
{
    private Stack<Action> _unregisterStack = new Stack<Action>();
    private StringBuilder sbLog = new StringBuilder();
    private List<IInitializable> _initList = new List<IInitializable>();

    // ==================================================================================
    // 1. 등록 단계 (Awake)
    // ==================================================================================
    private void Awake()
    {
        sbLog.Clear();
        sbLog.AppendLine("[SceneInitializer] 1. Manager Registration Started...");

        if (CheckDuplicateInitializer()) return;

        GameObject sceneRoot = GetOrCreateSceneRoot();

        try
        {
            // [Layer 1] System & Logic
            RegisterOrSpawn<TurnManager>(sceneRoot);
            RegisterOrSpawn<MapManager>(sceneRoot);
            RegisterOrSpawn<SessionManager>(sceneRoot);

            // [Layer 2] Combat
            RegisterOrSpawn<CombatManager>(sceneRoot);
            RegisterOrSpawn<PlayerInputCoordinator>(sceneRoot);
            RegisterOrSpawn<QTEManager>(sceneRoot);

            // [Layer 3] Visual / UI
            RegisterOrSpawn<TargetUIManager>(sceneRoot);
            RegisterOrSpawn<DamageTextManager>(sceneRoot);
            RegisterOrSpawn<PathVisualizer>(sceneRoot);
            RegisterOrSpawn<CameraController>(sceneRoot);

            // [Layer 4] Tool
            RegisterOrSpawn<TilemapGenerator>(sceneRoot);

            sbLog.AppendLine("[SceneInitializer] Registration Complete.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneInitializer] Critical Registration Error: {ex.Message}");
        }

        Debug.Log(sbLog.ToString());
    }

    // ==================================================================================
    // 2. 초기화 단계 (Start - 비동기)
    //    Awake에서 하지 않는 이유: 모든 매니저가 등록된 후 안전하게 참조하기 위함
    // ==================================================================================
    private void Start()
    {
        // 실제 비동기 로직을 호출합니다.
        InitializeSceneSequenceAsync().Forget();
    }

    /// <summary>
    /// 실제 비동기 초기화 시퀀스입니다.
    /// </summary>
    private async UniTaskVoid InitializeSceneSequenceAsync()
    {
        // [Bootstrap] Global 매니저가 없다면 AppInitializer가 완료될 때까지 대기
        if (!ServiceLocator.IsRegistered<GameManager>())
        {
            Debug.LogWarning("[SceneInitializer] Global Managers missing. Trying to Bootstrap...");

            // AppInitializer는 DontDestroyOnLoad이므로 어떤 씬에서든 찾을 수 있습니다.
            var appInit = FindObjectOfType<AppInitializer>();
            if (appInit != null)
            {
                // AppInitializer가 시동을 마칠 때까지 기다립니다.
                await UniTask.WaitUntil(() => appInit.CurrentState == AppInitializer.BootState.Complete);
            }
            else
            {
                Debug.LogError("[SceneInitializer] CRITICAL: AppInitializer not found!");
                return;
            }
        }

        // 씬 내부의 모든 매니저를 비동기로 초기화합니다.
        await InitializeAllManagersAsync();
    }
    private async UniTask InitializeAllManagersAsync()
    {
        sbLog.Clear();
        sbLog.AppendLine("[SceneInitializer] 2. Async Initialization Started...");

        var context = new InitializationContext
        {
            Scope = ManagerScope.Scene,
            GlobalSettings = ServiceLocator.Get<GlobalSettingsSO>()
        };

        foreach (var manager in _initList)
        {
            try
            {
                await manager.Initialize(context);
                sbLog.AppendLine($" - [Init] {manager.GetType().Name} Ready.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SceneInitializer] Error initializing {manager.GetType().Name}: {ex}");
            }
        }

        sbLog.AppendLine("[SceneInitializer] All Systems Operational.");
        Debug.Log(sbLog.ToString());

        if (ServiceLocator.TryGet<SessionManager>(out var session))
        {
            session.ChangeState(SessionState.Setup);
        }
    }

    private void OnDestroy()
    {
        sbLog.Clear();
        sbLog.AppendLine("[SceneInitializer] Cleanup sequence:");

        int count = 0;
        while (_unregisterStack.Count > 0)
        {
            _unregisterStack.Pop()?.Invoke();
            count++;
        }

        _initList.Clear();
        ServiceLocator.ClearScopeAsync(ManagerScope.Scene).Forget();

        sbLog.AppendLine($"[SceneInitializer] Unregistered {count} managers.");
        Debug.Log(sbLog.ToString());
    }

    // =========================================================
    // Helper Methods
    // =========================================================

    private void RegisterOrSpawn<T>(GameObject root) where T : MonoBehaviour
    {
        string typeName = typeof(T).Name;

        if (ServiceLocator.IsRegistered<T>())
        {
            sbLog.AppendLine($" - [Skip] {typeName} already registered.");
            return;
        }

        T[] existingInstances = FindObjectsOfType<T>(true);
        if (existingInstances.Length > 1)
            throw new InvalidOperationException($"[CRITICAL] Multiple {typeName} found in scene!");

        T instance;
        // [복구] 기존에 있던 건지, 새로 만든 건지 로그 분리
        if (existingInstances.Length == 1)
        {
            instance = existingInstances[0];
            sbLog.AppendLine($" - [Link] Found {typeName}."); // Found 로그 복구
        }
        else
        {
            instance = root.AddComponent<T>();
            sbLog.AppendLine($" - [Create] Spawned {typeName}."); // Spawned 로그 복구
        }

        if (instance == null)
        {
            Debug.LogError($"Failed to instance {typeName}.");
            return;
        }

        ServiceLocator.Register(instance, ManagerScope.Scene);

        if (instance is IInitializable initializable)
        {
            _initList.Add(initializable);
        }

        _unregisterStack.Push(() =>
        {
            ServiceLocator.Unregister<T>(ManagerScope.Scene);
        });
    }

    private bool CheckDuplicateInitializer()
    {
        var others = FindObjectsOfType<SceneInitializer>();
        if (others.Length > 1)
        {
            Debug.LogError("[SceneInitializer] Duplicate detected! Destroying self.");
            Destroy(gameObject);
            return true;
        }
        return false;
    }

    private GameObject GetOrCreateSceneRoot()
    {
        GameObject root = GameObject.Find("SceneManagers");
        if (root == null)
        {
            root = new GameObject("SceneManagers");
            // [복구] 하이어라키 정리를 위해 Initializer 자식으로 넣음
            root.transform.SetParent(this.transform);
        }
        return root;
    }
}