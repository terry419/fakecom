using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class TileDataManager : MonoBehaviour, IInitializable
{
    // [핵심] 씬 오브젝트가 아니라 '프리팹'에 미리 할당해야 합니다.
    [Header("Configuration")]
    [Tooltip("Project 창에서 TileDataManager 프리팹을 열고, TileRegistry 파일을 여기에 넣으세요.")]
    [SerializeField] private TileRegistrySO _registry;

    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 프리팹에 할당을 깜빡했을 경우를 대비한 안전장치
        if (_registry == null)
        {
            // 비상용: Resources 폴더에서라도 로드 시도 (경로는 프로젝트에 맞게 수정 가능)
            _registry = Resources.Load<TileRegistrySO>("Data/Map/TileRegistry");
        }

        if (_registry == null)
        {
            throw new BootstrapException(
                "[TileDataManager] CRITICAL: TileRegistrySO가 연결되지 않았습니다.\n" +
                "Action: Project 폴더의 TileDataManager 프리팹을 열고 인스펙터에 할당하십시오.");
        }

        _isInitialized = true;
        Debug.Log($"[TileDataManager] Initialized with Registry: {_registry.name}");
        await UniTask.CompletedTask;
    }

    // ========================================================================
    // 1. Data Accessors (데이터 조회)
    // ========================================================================

    public FloorEntry GetFloorData(FloorType type)
    {
        if (!_isInitialized || _registry == null) return default;
        return _registry.GetFloor(type);
    }

    public PillarEntry GetPillarData(PillarType type)
    {
        if (!_isInitialized || _registry == null) return default;
        return _registry.GetPillar(type);
    }

    public EdgeEntry GetEdgeData(EdgeType type)
    {
        if (!_isInitialized || _registry == null) return default;
        return _registry.GetEdge(type);
    }

    // ========================================================================
    // 2. Visual Accessors (TilemapGenerator 등에서 호출)
    // ========================================================================

    public GameObject GetFloorPrefab(FloorType type)
    {
        var entry = GetFloorData(type);
        if (type != FloorType.None && entry.Prefab == null)
        {
            Debug.LogWarning($"[TileDataManager] Missing prefab for Floor: {type}");
        }
        return entry.Prefab;
    }

    public GameObject GetPillarPrefab(PillarType type)
    {
        var entry = GetPillarData(type);
        if (type != PillarType.None && entry.Prefab == null)
        {
            Debug.LogWarning($"[TileDataManager] Missing prefab for Pillar: {type}");
        }
        return entry.Prefab;
    }

    // MapManager나 Generator에서 벽 프리팹 요청 시 사용
    public GameObject GetEdgePrefab(EdgeType type)
    {
        var entry = GetEdgeData(type);
        return entry.Prefab;
    }
}