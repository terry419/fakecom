using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SceneInitializer : MonoBehaviour, IInitializable
{
    // 자기 자신은 초기화 대상에서 제외해야 하므로, IInitializable을 구현하되 내용은 비워둡니다.
    public UniTask Initialize(InitializationContext context) => UniTask.CompletedTask;
    
    /// <summary>
    /// BootManager에 의해 호출되어 씬 초기화 프로세스를 시작합니다.
    /// </summary>
    public async UniTask InitializeSceneAsync(StringBuilder bootLog)
    {
        bootLog.AppendLine("\n-- Scene Initialization Sequence --");

        // 1. [수정] 자기 자신과 자식 오브젝트에 포함된 씬 매니저들을 모두 찾습니다.
        var sceneManagers = GetComponentsInChildren<IInitializable>(true)
            .Where(m => (UnityEngine.Object)m != this) // SceneInitializer 자기 자신은 제외
            .ToList();

        if (sceneManagers.Count == 0)
        {
            bootLog.AppendLine("No scene managers found in children. Skipping scene-level initialization.");
            return;
        }
        bootLog.AppendLine($"{sceneManagers.Count} scene managers found in hierarchy.");

        // 2. 의존성에 따라 매니저들을 정렬합니다.
        var sortedManagers = SortManagersByDependency(sceneManagers);
        bootLog.AppendLine("Dependency sort successful.");

        // 3. 테스트를 위한 기본 맵 데이터를 로드합니다.
        MapDataSO mapData;
        try
        {
            mapData = await Addressables.LoadAssetAsync<MapDataSO>("State1").ToUniTask();
            if (mapData == null) throw new NullReferenceException("Loaded MapDataSO 'State1' is null.");
            bootLog.AppendLine($"Default map 'State1' loaded successfully.");
        }
        catch (Exception ex)
        {
            throw new BootstrapException("Failed to load default map 'State1'.", ex);
        }
        
        // 4. 씬 컨텍스트를 생성하고 로드한 맵 데이터를 포함시킵니다.
        var sceneContext = new InitializationContext
        {
            Scope = ManagerScope.Scene,
            MapData = mapData
        };
        
        // 5. 정렬된 순서에 따라 순차적으로 초기화합니다.
        foreach (var manager in sortedManagers)
        {
            var managerName = manager.GetType().Name;
            bootLog.AppendLine($"- Initializing {managerName}...");
            try
            {
                await manager.Initialize(sceneContext);
                bootLog.AppendLine($"  └> {managerName} OK");
            }
            catch (Exception ex)
            {
                throw new BootstrapException($"Failed to initialize scene manager '{managerName}'.", ex);
            }
        }
    }

    private List<IInitializable> SortManagersByDependency(List<IInitializable> managers)
    {
        var managerMap = managers.ToDictionary(m => m.GetType(), m => m);
        var graph = managerMap.Keys.ToDictionary(t => t, t => new List<Type>());
        var inDegree = managerMap.Keys.ToDictionary(t => t, t => 0);

        foreach (var type in managerMap.Keys)
        {
            var attribute = type.GetCustomAttribute<DependsOnAttribute>();
            if (attribute == null) continue;

            foreach (var dependency in attribute.Dependencies)
            {
                if (managerMap.ContainsKey(dependency))
                {
                    graph[dependency].Add(type);
                    inDegree[type]++;
                }
                // [개선] 프리팹에 없는 의존성이 명시된 경우 에러 처리
                else
                {
                    throw new BootstrapException($"Dependency Error: '{type.Name}' depends on '{dependency.Name}', but it was not found in the scene manager prefab.");
                }
            }
        }

        var queue = new Queue<Type>(inDegree.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key));
        var sortedList = new List<IInitializable>();

        while (queue.Count > 0)
        {
            var type = queue.Dequeue();
            sortedList.Add(managerMap[type]);

            foreach (var dependent in graph[type])
            {
                inDegree[dependent]--;
                if (inDegree[dependent] == 0) queue.Enqueue(dependent);
            }
        }

        if (sortedList.Count != managers.Count)
        {
            var circular = managerMap.Keys.Except(sortedList.Select(m => m.GetType()));
            throw new InvalidOperationException($"Circular dependency detected: {string.Join(", ", circular.Select(t => t.Name))}");
        }

        return sortedList;
    }
}
