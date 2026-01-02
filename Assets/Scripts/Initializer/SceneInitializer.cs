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
    public UniTask Initialize(InitializationContext context) => UniTask.CompletedTask;

    public async UniTask InitializeSceneAsync(StringBuilder bootLog)
    {
        bootLog.AppendLine("\n-- Scene Initialization Sequence --");

        // 1. 자식 매니저 탐색
        var sceneManagers = GetComponentsInChildren<IInitializable>(true)
            .Where(m => (UnityEngine.Object)m != this)
            .ToList();

        if (sceneManagers.Count == 0)
        {
            bootLog.AppendLine("No scene managers found. Skipping.");
            return;
        }

        // 2. 의존성 정렬
        var sortedManagers = SortManagersByDependency(sceneManagers);

        // 3. 미션 정보 획득 (MissionManager)
        var missionMgr = ServiceLocator.Get<MissionManager>();
        MapEntry entry;

        if (missionMgr.SelectedMission.HasValue)
        {
            entry = missionMgr.SelectedMission.Value;
            bootLog.AppendLine($"Mission Selected: {entry.MapID}");
        }
        else
        {
            bootLog.AppendLine("No mission selected. Attempting fallback...");
            var catalog = ServiceLocator.Get<MapCatalogManager>();
            if (!catalog.TryGetRandomMapByDifficulty(1, out entry))
            {
                throw new BootstrapException("Failed to find any fallback map in Catalog.");
            }
            bootLog.AppendLine($"Fallback Map Loaded: {entry.MapID}");
        }

        // 4. [Fix] 맵 데이터 & 타일셋 병렬 로드 (에러 수정됨)
        bootLog.AppendLine($"- Loading Assets for {entry.MapID}...");

        var mapLoadTask = entry.MapDataRef.LoadAssetAsync().ToUniTask();

        // BiomeRegistryRef가 있으면 로드, 없으면 null 리턴 태스크
        UniTask<TileRegistrySO> biomeLoadTask = UniTask.FromResult<TileRegistrySO>(null);
        if (entry.BiomeRegistryRef != null && entry.BiomeRegistryRef.RuntimeKeyIsValid())
        {
            biomeLoadTask = entry.BiomeRegistryRef.LoadAssetAsync().ToUniTask();
        }

        // [핵심 수정] WhenAll의 결과를 튜플로 한 번에 받습니다. (개별 Task 재접근 금지)
        var (mapData, biomeRegistry) = await UniTask.WhenAll(mapLoadTask, biomeLoadTask);

        if (mapData == null) throw new BootstrapException($"Failed to load MapData for {entry.MapID}");

        // 5. 씬 컨텍스트 생성
        var sceneContext = new InitializationContext
        {
            Scope = ManagerScope.Scene,
            MapData = mapData,
            Registry = biomeRegistry // TileDataManager로 전달됨 (null일 수도 있음)
        };

        // 6. 매니저 초기화 루프
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

        if (sortedList.Count != managers.Count) return managers; // 순환 의존성 시 원본 반환
        return sortedList;
    }
}