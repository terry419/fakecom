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

        // 1. 매니저 탐색
        var sceneManagers = GetComponentsInChildren<IInitializable>(true)
            .Where(m => (UnityEngine.Object)m != this)
            .ToList();

        var sortedManagers = SortManagersByDependency(sceneManagers);

        // 2. [수정] 미션 정보 획득 로직 변경
        var missionMgr = ServiceLocator.Get<MissionManager>();

        MapDataSO mapData = null;
        TileRegistrySO biomeRegistry = null;

        // A. 정식 미션이 선택된 경우 (MissionManager에 데이터가 있음)
        if (missionMgr.SelectedMission != null) // [Fix] HasValue 제거, null 체크로 변경
        {
            var mission = missionMgr.SelectedMission;
            bootLog.AppendLine($"Mission Selected: {mission.MissionName}");

            // MissionDataSO가 이미 MapDataSO를 들고 있으므로 바로 사용
            mapData = mission.MapData;

            // MissionDataSO에 Biome 정보가 있다면 여기서 가져옴 (현재 구조상 없으면 null 처리)
            // 추후 MissionDataSO에 TileRegistrySO 필드도 추가하는 것을 권장
        }
        // B. 미션이 없는 경우 (테스트/Fallback) - 기존 MapEntry 방식 유지 (Catalog 사용)
        else
        {
            bootLog.AppendLine("No mission selected. Attempting fallback via Catalog...");
            var catalog = ServiceLocator.Get<MapCatalogManager>();

            if (catalog.TryGetRandomMapByDifficulty(1, out MapEntry entry))
            {
                bootLog.AppendLine($"Fallback Map Entry Found: {entry.MapID}");
                bootLog.AppendLine($"- Loading Assets via Addressables...");

                // Addressable 로드
                var mapLoadTask = entry.MapDataRef.LoadAssetAsync().ToUniTask();
                var biomeLoadTask = (entry.BiomeRegistryRef != null && entry.BiomeRegistryRef.RuntimeKeyIsValid())
                    ? entry.BiomeRegistryRef.LoadAssetAsync().ToUniTask()
                    : UniTask.FromResult<TileRegistrySO>(null);

                var results = await UniTask.WhenAll(mapLoadTask, biomeLoadTask);
                mapData = results.Item1;
                biomeRegistry = results.Item2;
            }
            else
            {
                // Catalog에도 없으면 MissionManager의 Inspector Fallback 확인
                // (이 부분은 MissionManager.Start에서 처리하므로 여기선 에러일 수 있음)
                // 하지만 로직 안전을 위해 null 체크 후 진행
            }
        }

        // 3. 맵 데이터 검증
        if (mapData == null)
        {
            throw new BootstrapException("Failed to load MapData! No Mission selected and no Fallback found.");
        }

        // 4. 씬 컨텍스트 생성
        var sceneContext = new InitializationContext
        {
            Scope = ManagerScope.Scene,
            MapData = mapData,
            Registry = biomeRegistry // null일 경우 기본값 사용됨
        };

        // 5. 매니저 초기화 루프
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

        // 6. [중요] MissionManager에게 "준비 끝, 미션 시작해" 신호 보내기 (옵션)
        // missionMgr.StartMissionAsync().Forget(); // 이미 Start()에서 호출되므로 생략 가능하나 순서상 여기가 맞음
    }

    // ... SortManagersByDependency는 기존 유지 ...
    private List<IInitializable> SortManagersByDependency(List<IInitializable> managers)
    {
        // (기존 코드와 동일)
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

        if (sortedList.Count != managers.Count) return managers;
        return sortedList;
    }
}