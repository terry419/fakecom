using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

// 경로: Assets / Scripts / Managers / Scene / PathVisualizer.cs

using UnityEngine;

using Cysharp.Threading.Tasks;

using System.Collections.Generic;

using System.Linq;



[RequireComponent(typeof(LineRenderer))]

public class PathVisualizer : MonoBehaviour, IInitializable

{

    [Header("Visual Resources")]

    [SerializeField] private PoolItem _reachablePrefab;

    [SerializeField] private PoolItem _pathPrefab;

    [SerializeField] private PoolItem _unreachablePrefab;



    [Header("Range Visualizer")]

    [SerializeField] private LineRenderer _lineRenderer;

    [SerializeField] private int _circleSegments = 50;



    // [Fix] 바닥 타일 유무와 상관없이 원이 잘 보이도록 높이 조절 (0.5 ~ 1.0 추천)

    [Tooltip("사거리 원이 그려질 높이 (유닛 발바닥 기준)")]

    [SerializeField] private float _rangeCircleOffset = 0.8f;



    [Header("Settings")]

    [SerializeField] private float _verticalOffset = 0.05f;

    [SerializeField] private int _initialPoolSize = 50;



    [Header("Z-Fighting Prevention")]

    [SerializeField] private float _zFightingOffset = 0.001f;

    [SerializeField] private bool _useRenderQueue = true;



    // ... (기존 변수들 유지: _poolRoot, _pools, _prefabs, List 등) ...

    private Transform _poolRoot;

    private bool _isInitialized = false;

    private Dictionary<PoolItem.PoolType, Queue<PoolItem>> _pools = new Dictionary<PoolItem.PoolType, Queue<PoolItem>>();

    private Dictionary<PoolItem.PoolType, PoolItem> _prefabs = new Dictionary<PoolItem.PoolType, PoolItem>();

    private List<PoolItem> _activeReachableItems = new List<PoolItem>();

    private List<PoolItem> _activePathItems = new List<PoolItem>();

    private Vector3[] _rangeCirclePositions;



    private void Awake()

    {

        if (ServiceLocator.IsRegistered<PathVisualizer>())

        {

            Destroy(gameObject);

            return;

        }

        ServiceLocator.Register(this, ManagerScope.Scene);

        if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();

    }



    private void OnDestroy()

    {

        if (ServiceLocator.IsRegistered<PathVisualizer>())

            ServiceLocator.Unregister<PathVisualizer>(ManagerScope.Scene);

    }



    public async UniTask Initialize(InitializationContext context)

    {

        if (_isInitialized) return;



        // ... (기존 초기화 로직 동일) ...

        if (_reachablePrefab == null || _pathPrefab == null || _unreachablePrefab == null)

            throw new BootstrapException("[PathVisualizer] Prefab(s) not assigned!");



        GameObject root = new GameObject("@VisualizerPool");

        root.transform.SetParent(transform);

        _poolRoot = root.transform;



        _prefabs[PoolItem.PoolType.Reachable] = _reachablePrefab;

        _prefabs[PoolItem.PoolType.Path] = _pathPrefab;

        _prefabs[PoolItem.PoolType.Unreachable] = _unreachablePrefab;



        _pools[PoolItem.PoolType.Reachable] = new Queue<PoolItem>();

        _pools[PoolItem.PoolType.Path] = new Queue<PoolItem>();

        _pools[PoolItem.PoolType.Unreachable] = new Queue<PoolItem>();



        ExpandPool(PoolItem.PoolType.Reachable, _initialPoolSize);

        ExpandPool(PoolItem.PoolType.Path, _initialPoolSize / 2);

        ExpandPool(PoolItem.PoolType.Unreachable, 10);



        if (_lineRenderer == null) _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer != null)

        {

            _lineRenderer.useWorldSpace = true;

            _lineRenderer.loop = true;

            _lineRenderer.enabled = false;

        }



        _isInitialized = true;

        await UniTask.CompletedTask;

    }



    // ========================================================================

    // [Fix] 사거리 표시 로직 수정

    // ========================================================================

    public void ShowRangeCircle(Vector3 center, int gridDistance)

    {

        if (_lineRenderer == null) return;



        _lineRenderer.enabled = true;



        float cellSize = 1.0f;

        try { cellSize = GridUtils.CELL_SIZE; } catch { }



        float worldRadius = gridDistance * cellSize;



        if (_rangeCirclePositions == null || _rangeCirclePositions.Length != _circleSegments)

        {

            _rangeCirclePositions = new Vector3[_circleSegments];

        }



        float angleStep = 360f / _circleSegments;



        for (int i = 0; i < _circleSegments; i++)

        {

            float angle = i * angleStep;

            float radian = Mathf.Deg2Rad * angle;



            float x = Mathf.Sin(radian) * worldRadius;

            float z = Mathf.Cos(radian) * worldRadius;



            // [Fix] 기존 _verticalOffset(0.05f) 대신 _rangeCircleOffset 사용

            // 타일이 없어도(구멍) 유닛 기준 높이에 그려지므로 끊기지 않음

            _rangeCirclePositions[i] = new Vector3(center.x + x, center.y + _rangeCircleOffset, center.z + z);

        }



        _lineRenderer.positionCount = _circleSegments;

        _lineRenderer.SetPositions(_rangeCirclePositions);

    }



    public void HideRangeCircle()

    {

        if (_lineRenderer != null) _lineRenderer.enabled = false;

    }



    // ... (이하 ShowReachable, ShowHybridPath, Pooling 등 기존 로직 동일 유지) ...

    public void ShowReachable(IEnumerable<GridCoords> coords, GridCoords? excludeCoords = null)

    {

        ClearReachable();

        if (coords == null) return;

        foreach (var c in coords)

        {

            if (excludeCoords.HasValue && c.Equals(excludeCoords.Value)) continue;

            SpawnItem(_pools[PoolItem.PoolType.Reachable], _activeReachableItems, c, 0, PoolItem.PoolType.Reachable);

        }

    }



    public void ShowHybridPath(IEnumerable<GridCoords> validPath, IEnumerable<GridCoords> invalidPath)

    {

        ClearPath();

        var validSet = validPath != null ? new HashSet<GridCoords>(validPath) : new HashSet<GridCoords>();

        var invalidSet = invalidPath != null ? new HashSet<GridCoords>(invalidPath) : new HashSet<GridCoords>();



        foreach (var c in validSet) SpawnItem(_pools[PoolItem.PoolType.Path], _activePathItems, c, 1, PoolItem.PoolType.Path);

        foreach (var c in invalidSet)

        {

            if (!validSet.Contains(c)) SpawnItem(_pools[PoolItem.PoolType.Unreachable], _activePathItems, c, 1, PoolItem.PoolType.Unreachable);

        }

    }



    public void ClearAll() { ClearReachable(); ClearPath(); HideRangeCircle(); }

    public void ClearPath() { foreach (var item in _activePathItems) ReturnToPool(item); _activePathItems.Clear(); }

    private void ClearReachable() { foreach (var item in _activeReachableItems) ReturnToPool(item); _activeReachableItems.Clear(); }



    private void ReturnToPool(PoolItem item)

    {

        item.gameObject.SetActive(false);

        if (_pools.ContainsKey(item.Type)) _pools[item.Type].Enqueue(item);

        else Destroy(item.gameObject);

    }



    private void SpawnItem(Queue<PoolItem> pool, List<PoolItem> activeList, GridCoords coords, int layerIndex, PoolItem.PoolType type)

    {

        Vector3 worldPos = GridUtils.GridToWorld(coords);

        PoolItem item = GetFromPool(type);

        if (item == null) return;



        if (_useRenderQueue && item.TryGetComponent<Renderer>(out var renderer))

        {

            int baseQueue = type switch { PoolItem.PoolType.Reachable => 2000, PoolItem.PoolType.Path => 2100, PoolItem.PoolType.Unreachable => 2050, _ => 2000 };

            renderer.material.renderQueue = baseQueue;

            worldPos.y += _verticalOffset;

        }

        else { worldPos.y += _verticalOffset + (layerIndex * _zFightingOffset); }



        item.transform.position = worldPos;

        item.gameObject.SetActive(true);

        activeList.Add(item);

    }



    private PoolItem GetFromPool(PoolItem.PoolType type)

    {

        if (_pools.TryGetValue(type, out var queue) && queue.Count > 0) return queue.Dequeue();

        if (_prefabs.TryGetValue(type, out var prefab) && prefab != null)

        {

            var newItem = Instantiate(prefab, _poolRoot);

            newItem.Type = type;

            return newItem;

        }

        return null;

    }



    private void ExpandPool(PoolItem.PoolType type, int count)

    {

        if (!_prefabs.TryGetValue(type, out var prefab)) return;

        if (!_pools.TryGetValue(type, out var queue)) return;

        for (int i = 0; i < count; i++) { var item = Instantiate(prefab, _poolRoot); item.Type = type; item.gameObject.SetActive(false); queue.Enqueue(item); }

    }

}