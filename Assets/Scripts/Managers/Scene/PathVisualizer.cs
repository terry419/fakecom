// 경로: Assets/Scripts/Managers/Scene/PathVisualizer.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class PathVisualizer : MonoBehaviour, IInitializable
{
    [Header("Visual Resources")]
    [SerializeField] private PoolItem _reachablePrefab;   // 녹색 (Inspector 할당)
    [SerializeField] private PoolItem _pathPrefab;        // 파란색 (Inspector 할당)
    [SerializeField] private PoolItem _unreachablePrefab; // 빨간색 (Inspector 할당)

    [Header("Settings")]
    [SerializeField] private float _verticalOffset = 0.05f;
    [SerializeField] private int _initialPoolSize = 50;

    private Transform _poolRoot;

    // 풀
    private Queue<PoolItem> _reachablePool = new Queue<PoolItem>();
    private Queue<PoolItem> _pathPool = new Queue<PoolItem>();
    private Queue<PoolItem> _unreachablePool = new Queue<PoolItem>();

    // 활성 리스트
    private List<PoolItem> _activeReachableItems = new List<PoolItem>();
    private List<PoolItem> _activePathItems = new List<PoolItem>(); // 파랑/빨강 경로 모두 관리

    private void Awake()
    {
        if (ServiceLocator.IsRegistered<PathVisualizer>())
        {
            Destroy(gameObject);
            return;
        }
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<PathVisualizer>())
            ServiceLocator.Unregister<PathVisualizer>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        GameObject root = new GameObject("@VisualizerPool");
        root.transform.SetParent(transform);
        _poolRoot = root.transform;

        ExpandPool(_reachablePool, _reachablePrefab, _initialPoolSize, PoolItem.PoolType.Reachable);
        ExpandPool(_pathPool, _pathPrefab, _initialPoolSize / 2, PoolItem.PoolType.Path);
        ExpandPool(_unreachablePool, _unreachablePrefab, 10, PoolItem.PoolType.Unreachable);

        await UniTask.CompletedTask;
    }

    // [이동 가능 범위 표시] - 녹색
    public void ShowReachable(IEnumerable<GridCoords> coords, GridCoords? excludeCoords = null)
    {
        ClearReachable();

        if (coords == null) return;

        foreach (var c in coords)
        {
            if (excludeCoords.HasValue && c.Equals(excludeCoords.Value)) continue;
            SpawnItem(_reachablePool, _activeReachableItems, c, 0, PoolItem.PoolType.Reachable);
        }
    }

    // [Hybrid Path 표시] - 유효(파랑) + 무효(빨강) 동시 표시
    public void ShowHybridPath(List<GridCoords> validPath, List<GridCoords> invalidPath)
    {
        ClearPath(); // 기존 경로 삭제 (녹색은 유지)

        // 1. 유효 경로 (파란색)
        if (validPath != null)
        {
            foreach (var c in validPath)
            {
                SpawnItem(_pathPool, _activePathItems, c, 1, PoolItem.PoolType.Path);
            }
        }

        // 2. 무효 경로 (빨간색)
        if (invalidPath != null)
        {
            foreach (var c in invalidPath)
            {
                SpawnItem(_unreachablePool, _activePathItems, c, 1, PoolItem.PoolType.Unreachable);
            }
        }
    }

    public void ClearAll()
    {
        ClearReachable();
        ClearPath();
    }

    public void ClearPath() // Public으로 열어서 Controller에서 호출 가능하게 함
    {
        foreach (var item in _activePathItems)
        {
            ReturnToPool(item);
        }
        _activePathItems.Clear();
    }

    private void ClearReachable()
    {
        foreach (var item in _activeReachableItems)
        {
            ReturnToPool(item);
        }
        _activeReachableItems.Clear();
    }

    private void ReturnToPool(PoolItem item)
    {
        item.gameObject.SetActive(false);
        switch (item.Type)
        {
            case PoolItem.PoolType.Reachable: _reachablePool.Enqueue(item); break;
            case PoolItem.PoolType.Path: _pathPool.Enqueue(item); break;
            case PoolItem.PoolType.Unreachable: _unreachablePool.Enqueue(item); break;
            default: Destroy(item.gameObject); break;
        }
    }

    private void SpawnItem(Queue<PoolItem> pool, List<PoolItem> activeList, GridCoords coords, int layerIndex, PoolItem.PoolType type)
    {
        Vector3 worldPos = GridUtils.GridToWorld(coords);
        PoolItem item = GetFromPool(pool, type);

        // Z-Fighting 방지
        worldPos.y += _verticalOffset + (layerIndex * 0.01f);

        item.transform.position = worldPos;
        item.gameObject.SetActive(true);

        activeList.Add(item);
    }

    private PoolItem GetFromPool(Queue<PoolItem> pool, PoolItem.PoolType type)
    {
        if (pool.Count > 0) return pool.Dequeue();

        PoolItem prefab = type == PoolItem.PoolType.Reachable ? _reachablePrefab :
                          type == PoolItem.PoolType.Path ? _pathPrefab : _unreachablePrefab;

        var newItem = Instantiate(prefab, _poolRoot);
        newItem.Type = type;
        return newItem;
    }

    private void ExpandPool(Queue<PoolItem> pool, PoolItem prefab, int count, PoolItem.PoolType type)
    {
        for (int i = 0; i < count; i++)
        {
            var item = Instantiate(prefab, _poolRoot);
            item.Type = type;
            item.gameObject.SetActive(false);
            pool.Enqueue(item);
        }
    }
}