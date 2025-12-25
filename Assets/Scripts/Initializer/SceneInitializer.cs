using UnityEngine;
using System;
using System.Collections.Generic;
using System.Text;

public class SceneInitializer : MonoBehaviour
{
    private Stack<Action> _unregisterStack = new Stack<Action>();
    private StringBuilder sbLog = new StringBuilder();

    // [추가된 부분] 초기화가 필요한 매니저들을 줄 세울 리스트
    private List<IInitializable> _initList = new List<IInitializable>();

    private void Awake()
    {
        sbLog.Clear();
        sbLog.AppendLine("[SceneInitializer] Start Initialization...");

        if (CheckDuplicateInitializer()) return;
        GameObject sceneRoot = GetOrCreateSceneRoot();

        // 1. 매니저 등록 (여기서는 생성과 등록만 함)
        try
        {
            // [System Layer]
            RegisterOrSpawn<TurnManager>(sceneRoot);
            RegisterOrSpawn<MapManager>(sceneRoot);

            // [Logic Layer]
            RegisterOrSpawn<SessionManager>(sceneRoot); // 세션 매니저 등록
            RegisterOrSpawn<CombatManager>(sceneRoot);
            RegisterOrSpawn<PlayerInputCoordinator>(sceneRoot);
            RegisterOrSpawn<QTEManager>(sceneRoot);

            // [Visual/UI Layer]
            RegisterOrSpawn<TargetUIManager>(sceneRoot);
            RegisterOrSpawn<DamageTextManager>(sceneRoot);
            RegisterOrSpawn<PathVisualizer>(sceneRoot);
            RegisterOrSpawn<CameraController>(sceneRoot);

            // [Tool Layer]
            RegisterOrSpawn<TilemapGenerator>(sceneRoot);

            // 2. [추가된 부분] 일괄 초기화 실행 (모든 매니저가 등록된 후 안전하게 실행)
            InitializeAllManagers();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SceneInitializer] Critical Error: {ex.Message}");
            // 여기서 치명적 에러면 SessionManager를 Error 상태로 보내거나 게임 종료
        }

        Debug.Log(sbLog.ToString());
    }

    private void OnDestroy()
    {
        sbLog.Clear();
        int count = 0;
        sbLog.AppendLine("[SceneInitializer] Cleanup sequence:");

        while (_unregisterStack.Count > 0)
        {
            Action unregisterAction = _unregisterStack.Pop();
            unregisterAction?.Invoke();
            count++;
        }

        // 리스트도 비워줌
        _initList.Clear();

        sbLog.AppendLine($"[SceneInitializer] Cleaned up {count} managers.");
        Debug.Log(sbLog.ToString());
    }

    // =========================================================
    // Helper Methods
    // =========================================================

    private void InitializeAllManagers()
    {
        sbLog.AppendLine(" --- Starting Manager Initialization ---");

        // 리스트에 담긴 순서대로 초기화 (등록 순서 = 초기화 순서)
        foreach (var manager in _initList)
        {
            try
            {
                manager.Initialize();
                sbLog.AppendLine($" - [Init] {manager.GetType().Name} Initialized.");
            }
            catch (Exception ex)
            {
                // 초기화 도중 에러가 나면 즉시 보고
                Debug.LogError($"[SceneInitializer] Error initializing {manager.GetType().Name}: {ex.Message}");
                throw; // 상위 catch로 던짐
            }
        }
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
            root.transform.SetParent(this.transform);
        }
        return root;
    }

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
            throw new InvalidOperationException($"[CRITICAL] Multiple {typeName} found!");

        T instance = (existingInstances.Length == 1) ? existingInstances[0] : root.AddComponent<T>();

        if (instance == null)
        {
            Debug.LogError($"Failed to instance {typeName}.");
            return;
        }
        else if (existingInstances.Length == 0)
        {
            sbLog.AppendLine($" - [Create] Spawned {typeName}.");
        }
        else
        {
            sbLog.AppendLine($" - [Link] Found {typeName}.");
        }

        // ServiceLocator 등록
        ServiceLocator.Register(instance);

        // [핵심] IInitializable 인터페이스가 있다면 초기화 대기열(_initList)에 추가
        if (instance is IInitializable initializable)
        {
            _initList.Add(initializable);
        }

        // 해제 예약
        _unregisterStack.Push(() =>
        {
            ServiceLocator.Unregister<T>();
            sbLog.AppendLine($"   - [Unregister] {typeName}");
        });
    }
}