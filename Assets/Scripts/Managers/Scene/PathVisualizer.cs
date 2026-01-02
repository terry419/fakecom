using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class PathVisualizer : MonoBehaviour, IInitializable
{
    [Header("Visual Resources")]
    [SerializeField] private PoolItem _reachablePrefab;   // 녹색 타일 (이동 가능 범위)
    [SerializeField] private PoolItem _pathPrefab;        // 파란색 타일 (이동 경로)
    [SerializeField] private PoolItem _unreachablePrefab; // 빨간색 타일 (이동 불가/초과)

    [Header("Settings")]
    [SerializeField] private float _verticalOffset = 0.05f;
    [SerializeField] private int _initialPoolSize = 50;

    // --- Object Pooling ---
    private Transform _poolRoot;

    // 대기열 (Pool)
    private Queue<PoolItem> _reachablePool = new Queue<PoolItem>();
    private Queue<PoolItem> _pathPool = new Queue<PoolItem>();
    private Queue<PoolItem> _unreachablePool = new Queue<PoolItem>();

    // 활성 리스트 (Active List)
    private List<PoolItem> _activeReachables = new List<PoolItem>();
    private List<PoolItem> _activePaths = new List<PoolItem>(); // 파랑/빨강 섞임

    private void Awake()
    {
        // [핵심 수정] 다른 매니저들처럼 Awake 등록. 
        // 단, 중복 생성된 경우(SceneInitializer 충돌 방지) 스스로 파괴.
        if (ServiceLocator.IsRegistered<PathVisualizer>())
        {
            Debug.LogWarning("[PathVisualizer] 중복 발견됨. 이 오브젝트를 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<PathVisualizer>())
        {
            ServiceLocator.Unregister<PathVisualizer>(ManagerScope.Scene);
        }
    }

    public async UniTask Initialize(InitializationContext context)
    {
        GameObject root = new GameObject("@VisualizerPool");
        root.transform.SetParent(transform);
        _poolRoot = root.transform;

        // 풀링 초기화
        ExpandPool(_reachablePool, _reachablePrefab, _initialPoolSize, PoolItem.PoolType.Reachable);
        ExpandPool(_pathPool, _pathPrefab, _initialPoolSize / 2, PoolItem.PoolType.Path);
        ExpandPool(_unreachablePool, _unreachablePrefab, 10, PoolItem.PoolType.Unreachable);

        await UniTask.CompletedTask;
    }

    // ========================================================================
    // 1. 이동 가능 범위 (녹색)
    // ========================================================================
    public void ShowReachableTiles(IEnumerable<GridCoords> coords)
    {
        ClearReachable();

        if (coords == null || _reachablePrefab == null) return;

        foreach (var c in coords)
        {
            SpawnItem(_reachablePool, _reachablePrefab, _activeReachables, c, 0);
        }
    }

    public void ClearReachable()
    {
        foreach (var item in _activeReachables)
        {
            if (item != null)
            {
                item.gameObject.SetActive(false);
                _reachablePool.Enqueue(item);
            }
        }
        _activeReachables.Clear();
    }

    // ========================================================================
    // 2. 경로 표시 (파랑/빨강)
    // ========================================================================
    public void ShowPath(List<GridCoords> path, int currentAP)
    {
        ClearPath(); // 기존 경로 정리

        if (path == null || path.Count == 0) return;

        for (int i = 0; i < path.Count; i++)
        {
            GridCoords c = path[i];

            // [로직] 인덱스(거리)가 현재 AP보다 작으면 파랑, 같거나 크면 빨강
            // 예: AP 5일 때, index 0~4(5칸)는 파랑, index 5(6번째 칸)부터 빨강
            bool isReachable = (i < currentAP);

            var targetPool = isReachable ? _pathPool : _unreachablePool;
            var targetPrefab = isReachable ? _pathPrefab : _unreachablePrefab;

            SpawnItem(targetPool, targetPrefab, _activePaths, c, 1);
        }
    }

    public void ClearPath()
    {
        // [중요] 섞여 있는 아이템들을 제집(Type) 찾아 반납
        foreach (var item in _activePaths)
        {
            if (item == null) continue;

            item.gameObject.SetActive(false);

            switch (item.Type)
            {
                case PoolItem.PoolType.Path:
                    _pathPool.Enqueue(item);
                    break;
                case PoolItem.PoolType.Unreachable:
                    _unreachablePool.Enqueue(item);
                    break;
                default:
                    Destroy(item.gameObject);
                    break;
            }
        }
        _activePaths.Clear();
    }

    // ========================================================================
    // 3. Pooling Internals
    // ========================================================================
    private void SpawnItem(Queue<PoolItem> pool, PoolItem prefab, List<PoolItem> activeList, GridCoords coords, int layerIndex)
    {
        PoolItem item = GetFromPool(pool, prefab);
        if (item == null) return;

        Vector3 pos = GridUtils.GridToWorld(coords);
        pos.y += _verticalOffset + (layerIndex * 0.01f); // 겹침 방지

        item.transform.position = pos;
        item.gameObject.SetActive(true);

        activeList.Add(item);
    }

    private PoolItem GetFromPool(Queue<PoolItem> pool, PoolItem prefab)
    {
        if (prefab == null) return null;

        if (pool.Count > 0)
        {
            var item = pool.Dequeue();
            if (item != null) return item;
        }

        return CreateNewItem(prefab);
    }

    private void ExpandPool(Queue<PoolItem> pool, PoolItem prefab, int count, PoolItem.PoolType type)
    {
        if (prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            var item = CreateNewItem(prefab);
            item.Type = type;
            pool.Enqueue(item);
        }
    }

    private PoolItem CreateNewItem(PoolItem prefab)
    {
        var item = Instantiate(prefab, _poolRoot);
        item.name = prefab.name;
        item.gameObject.SetActive(false);

        // 생성 시 타입 확정
        if (prefab == _pathPrefab) item.Type = PoolItem.PoolType.Path;
        else if (prefab == _unreachablePrefab) item.Type = PoolItem.PoolType.Unreachable;
        else item.Type = PoolItem.PoolType.Reachable;

        return item;
    }
}