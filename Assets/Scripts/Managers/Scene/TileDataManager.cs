using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class TileDataManager : MonoBehaviour, IInitializable
{
    [Header("Configuration")]
    [Tooltip("Project 창에서 TileDataManager 프리팹을 열고, 기본 TileRegistry 파일을 여기에 넣으세요 (Fallback용).")]
    [SerializeField] private TileRegistrySO _registry;

    private bool _isInitialized = false;

    // [중요] Global이 아니라 Scene 스코프로 등록해야 합니다.
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 1. 미션(Context)에서 넘어온 타일셋이 있으면 최우선 적용 (Override)
        if (context.Registry != null)
        {
            _registry = context.Registry;
        }

        // 2. 프리팹 할당도 없고, Context도 없으면 비상용 로드
        if (_registry == null)
        {
            // 경로가 맞는지 확인 필요
            _registry = Resources.Load<TileRegistrySO>("Data/Map/TileRegistry");
        }

        // 3. 그래도 없으면 에러
        if (_registry == null)
        {
            throw new BootstrapException(
                "[TileDataManager] CRITICAL: TileRegistrySO가 연결되지 않았습니다.\n" +
                "Action: Project 폴더의 TileDataManager 프리팹을 열고 인스펙터에 할당하거나 MapEntry의 BiomeRef를 확인하십시오.");
        }

        _isInitialized = true;
        // Debug.Log($"[TileDataManager] Initialized with Registry: {_registry.name}");
        await UniTask.CompletedTask;
    }

    // ========================================================================
    // Data Accessors
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
    // Visual Accessors
    // ========================================================================

    public GameObject GetFloorPrefab(FloorType type)
    {
        var entry = GetFloorData(type);
        if (type != FloorType.None && entry.Prefab == null)
        {
            // Debug.LogWarning($"[TileDataManager] Missing prefab for Floor: {type}");
        }
        return entry.Prefab;
    }

    public GameObject GetPillarPrefab(PillarType type)
    {
        var entry = GetPillarData(type);
        if (type != PillarType.None && entry.Prefab == null)
        {
            // Debug.LogWarning($"[TileDataManager] Missing prefab for Pillar: {type}");
        }
        return entry.Prefab;
    }

    public GameObject GetEdgePrefab(EdgeType type)
    {
        var entry = GetEdgeData(type);
        return entry.Prefab;
    }
}