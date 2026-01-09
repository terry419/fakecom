using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class TileDataManager : MonoBehaviour, IInitializable
{
    [Header("Configuration")]
    [Tooltip("Project 창에서 TileDataManager 프리팹을 열고, 기본 TileRegistry 파일을 여기에 넣으세요 (Fallback용).")]
    [SerializeField] private TileRegistrySO _registry;

    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        if (context.Registry != null)
        {
            _registry = context.Registry;
        }

        if (_registry == null)
        {
            _registry = Resources.Load<TileRegistrySO>("Data/Map/TileRegistry");
        }

        if (_registry == null)
        {
            throw new BootstrapException(
                "[TileDataManager] CRITICAL: TileRegistrySO가 연결되지 않았습니다.\n" +
                "1. GlobalSettings에 할당되었는지\n" +
                "2. Resources/Data/Map/TileRegistry 경로에 파일이 있는지 확인하세요.");
        }

        _isInitialized = true;
        Debug.Log($"[TileDataManager] Initialized. Registry: {_registry.name}");
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
        return entry.Prefab;
    }

    public GameObject GetPillarPrefab(PillarType type)
    {
        var entry = GetPillarData(type);
        return entry.Prefab;
    }

    public GameObject GetEdgePrefab(EdgeType type)
    {
        var entry = GetEdgeData(type);
        return entry.Prefab;
    }

    // [Fix] EnvironmentManager에서 사용할 포탈 프리팹 조회 메서드 추가
    public GameObject GetPortalPrefab(PortalType type)
    {
        if (!_isInitialized || _registry == null) return null;
        // TileRegistrySO의 GetPortalPrefab은 PortalEntry를 반환하므로 .Prefab 접근
        return _registry.GetPortalPrefab(type).Prefab;
    }

    // [Fix] 레지스트리 직접 접근이 필요하다면 사용 (Optional)
    public TileRegistrySO GetRegistry() => _registry;
}