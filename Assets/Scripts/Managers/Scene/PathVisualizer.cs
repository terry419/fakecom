using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] private float _rangeCircleOffset = 0.5f; // 높이 보정

    [Header("Settings")]
    [SerializeField] private float _verticalOffset = 0.05f;
    [SerializeField] private int _initialPoolSize = 50;
    [SerializeField] private float _zFightingOffset = 0.001f;
    [SerializeField] private bool _useRenderQueue = true;

    private Transform _poolRoot;
    private bool _isInitialized = false;

    // 풀링 시스템
    private Dictionary<PoolItem.PoolType, Queue<PoolItem>> _pools = new Dictionary<PoolItem.PoolType, Queue<PoolItem>>();
    private Dictionary<PoolItem.PoolType, PoolItem> _prefabs = new Dictionary<PoolItem.PoolType, PoolItem>();

    // 활성화된 아이템 추적
    private List<PoolItem> _activeReachableItems = new List<PoolItem>();
    private List<PoolItem> _activePathItems = new List<PoolItem>();
    private Vector3[] _rangeCirclePositions;

    private void Awake()
    {
        // 중복 등록 방지
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

        GameObject root = new GameObject("@VisualizerPool");
        root.transform.SetParent(transform);
        _poolRoot = root.transform;

        // 딕셔너리 초기화
        _prefabs[PoolItem.PoolType.Reachable] = _reachablePrefab;
        _prefabs[PoolItem.PoolType.Path] = _pathPrefab;
        _prefabs[PoolItem.PoolType.Unreachable] = _unreachablePrefab;

        _pools[PoolItem.PoolType.Reachable] = new Queue<PoolItem>();
        _pools[PoolItem.PoolType.Path] = new Queue<PoolItem>();
        _pools[PoolItem.PoolType.Unreachable] = new Queue<PoolItem>();

        // 풀 확장
        ExpandPool(PoolItem.PoolType.Reachable, _initialPoolSize);
        ExpandPool(PoolItem.PoolType.Path, _initialPoolSize / 2);
        ExpandPool(PoolItem.PoolType.Unreachable, 10);

        // LineRenderer 초기 설정
        if (_lineRenderer != null)
        {
            _lineRenderer.useWorldSpace = true;
            _lineRenderer.loop = true;
            _lineRenderer.enabled = false;
            _lineRenderer.startWidth = 0.05f; // 기본 두께
            _lineRenderer.endWidth = 0.05f;
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // 기본 재질
            _lineRenderer.startColor = Color.red; // 기본 색상
            _lineRenderer.endColor = Color.red;
        }

        _isInitialized = true;
        await UniTask.CompletedTask;
    }

    // ========================================================================
    // [공용] 사거리 원 그리기 (순수 데이터 기반)
    // ========================================================================
    public void ShowRangeCircle(Vector3 centerWorldPos, float radiusWorld)
    {
        if (_lineRenderer == null) return;
        _lineRenderer.enabled = true;

        if (_rangeCirclePositions == null || _rangeCirclePositions.Length != _circleSegments)
        {
            _rangeCirclePositions = new Vector3[_circleSegments];
        }

        float angleStep = 360f / _circleSegments;
        for (int i = 0; i < _circleSegments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radiusWorld;
            float z = Mathf.Cos(angle) * radiusWorld;

            // 높이 보정 적용
            _rangeCirclePositions[i] = new Vector3(centerWorldPos.x + x, centerWorldPos.y + _rangeCircleOffset, centerWorldPos.z + z);
        }

        _lineRenderer.positionCount = _circleSegments;
        _lineRenderer.SetPositions(_rangeCirclePositions);
    }

    public void HideRangeCircle()
    {
        if (_lineRenderer != null) _lineRenderer.enabled = false;
    }

    // ========================================================================
    // [공용] 이동 가능 타일 표시
    // ========================================================================
    public void ShowReachable(IEnumerable<GridCoords> coords, GridCoords? excludeCoords = null)
    {
        ClearReachable();
        if (coords == null) return;

        foreach (var c in coords)
        {
            if (excludeCoords.HasValue && c.Equals(excludeCoords.Value)) continue;
            SpawnItem(_activeReachableItems, c, 0, PoolItem.PoolType.Reachable);
        }
    }

    // ========================================================================
    // [공용] 경로 표시 (유효/무효 구분)
    // ========================================================================
    public void ShowHybridPath(IEnumerable<GridCoords> validPath, IEnumerable<GridCoords> invalidPath)
    {
        ClearPath();

        var validSet = validPath != null ? new HashSet<GridCoords>(validPath) : new HashSet<GridCoords>();
        var invalidSet = invalidPath != null ? new HashSet<GridCoords>(invalidPath) : new HashSet<GridCoords>();

        foreach (var c in validSet) SpawnItem(_activePathItems, c, 1, PoolItem.PoolType.Path);
        foreach (var c in invalidSet)
        {
            if (!validSet.Contains(c)) SpawnItem(_activePathItems, c, 1, PoolItem.PoolType.Unreachable);
        }
    }

    public void ClearAll() { ClearReachable(); ClearPath(); HideRangeCircle(); }
    public void ClearPath() { RecycleItems(_activePathItems); }
    private void ClearReachable() { RecycleItems(_activeReachableItems); }

    // --- 내부 풀링 로직 ---
    private void RecycleItems(List<PoolItem> activeList)
    {
        foreach (var item in activeList)
        {
            item.gameObject.SetActive(false);
            if (_pools.ContainsKey(item.Type)) _pools[item.Type].Enqueue(item);
            else Destroy(item.gameObject);
        }
        activeList.Clear();
    }

    private void SpawnItem(List<PoolItem> activeList, GridCoords coords, int layerIndex, PoolItem.PoolType type)
    {
        PoolItem item = GetFromPool(type);
        if (item == null) return;

        Vector3 worldPos = GridUtils.GridToWorld(coords);

        if (_useRenderQueue && item.TryGetComponent<Renderer>(out var renderer))
        {
            int baseQueue = type switch
            {
                PoolItem.PoolType.Reachable => 2000,
                PoolItem.PoolType.Path => 2100,
                PoolItem.PoolType.Unreachable => 2050,
                _ => 2000
            };
            renderer.material.renderQueue = baseQueue;
            worldPos.y += _verticalOffset;
        }
        else
        {
            worldPos.y += _verticalOffset + (layerIndex * _zFightingOffset);
        }

        item.transform.position = worldPos;
        item.gameObject.SetActive(true);
        activeList.Add(item);
    }

    private PoolItem GetFromPool(PoolItem.PoolType type)
    {
        if (!_pools.ContainsKey(type)) return null;

        if (_pools[type].Count == 0) ExpandPool(type, 5); // 부족하면 즉시 확장

        var item = _pools[type].Dequeue();
        return item;
    }

    private void ExpandPool(PoolItem.PoolType type, int count)
    {
        if (!_prefabs.TryGetValue(type, out var prefab) || prefab == null) return;
        for (int i = 0; i < count; i++)
        {
            var item = Instantiate(prefab, _poolRoot);
            item.Type = type;
            item.gameObject.SetActive(false);
            _pools[type].Enqueue(item);
        }
    }
}