using UnityEngine;
using Cysharp.Threading.Tasks;

[DependsOn(typeof(MapManager), typeof(TileDataManager))]
public class EnvironmentManager : MonoBehaviour, IInitializable
{
    private MapManager _mapManager;
    private TileDataManager _tileDataManager;

    // [Fix] 포탈들을 묶어서 관리할 부모 트랜스폼
    private Transform _portalParent;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<EnvironmentManager>(ManagerScope.Scene);

    public async UniTask Initialize(InitializationContext context)
    {
        _mapManager = ServiceLocator.Get<MapManager>();
        _tileDataManager = ServiceLocator.Get<TileDataManager>();

        BuildMapFeatures();
        await UniTask.CompletedTask;
    }

    public void BuildMapFeatures()
    {
        if (_mapManager == null) return;

        Debug.Log("[EnvironmentManager] Start Building Features...");

        // [Fix] 포탈 부모 생성 (씬 정리용)
        if (_portalParent == null)
        {
            var go = new GameObject("Runtime_Portals");
            go.transform.SetParent(this.transform);
            _portalParent = go.transform;
        }

        foreach (Tile tile in _mapManager.GetAllTiles())
        {
            ProcessPillar(tile);
            LinkEdgesForTile(tile);
            ProcessPortal(tile);
        }

        Debug.Log("[EnvironmentManager] Build Complete.");
    }

    private void ProcessPillar(Tile tile)
    {
        if (tile.InitialPillarID == PillarType.None) return;

        var pillarData = _tileDataManager.GetPillarData(tile.InitialPillarID);
        float currentHP = (tile.InitialPillarHP > 0) ? tile.InitialPillarHP : pillarData.MaxHP;

        PillarInfo pillar = new PillarInfo(tile.InitialPillarID, pillarData.MaxHP, currentHP);
        tile.AddOccupant(pillar);
    }

    private void LinkEdgesForTile(Tile tile)
    {
        GridCoords coords = tile.Coordinate;

        // North (z + 1)
        ProcessSingleEdge(tile, Direction.North, new GridCoords(coords.x, coords.z + 1, coords.y), Direction.South);

        // East (x + 1)
        ProcessSingleEdge(tile, Direction.East, new GridCoords(coords.x + 1, coords.z, coords.y), Direction.West);
    }

    private void ProcessSingleEdge(Tile currentTile, Direction currentDir, GridCoords neighborPos, Direction neighborDir)
    {
        SavedEdgeInfo info = currentTile.TempSavedEdges[(int)currentDir];

        var edgeDataEntry = _tileDataManager.GetEdgeData(info.Type);
        bool isPassable = edgeDataEntry.IsPassable;

        RuntimeEdge edge = new RuntimeEdge(info.Type, info.Cover, info.MaxHP, info.CurrentHP, isPassable);

        currentTile.SetSharedEdge(currentDir, edge);

        Tile neighbor = _mapManager.GetTile(neighborPos);
        if (neighbor != null)
        {
            neighbor.SetSharedEdge(neighborDir, edge);
        }
    }

    // [Fix] 포탈 생성 로직 수정
    private void ProcessPortal(Tile tile)
    {
        if (tile.PortalData == null) return;

        // 1. GetRegistry() 대신 GetPortalPrefab() 사용 (CS1061 해결)
        GameObject prefab = _tileDataManager.GetPortalPrefab(tile.PortalData.Type);
        if (prefab == null) return;

        Vector3 worldPos = GridUtils.GridToWorld(tile.Coordinate);
        GameObject portalObj = Instantiate(prefab, worldPos, Quaternion.identity);

        // 2. tile.GetTransform() 대신 _portalParent 사용 (CS1061 해결)
        portalObj.transform.SetParent(_portalParent);

        // Out 포탈 회전 처리
        if (tile.PortalData.Type == PortalType.Out)
        {
            GridCoords dirCoords = GridUtils.GetDirectionVector(tile.PortalData.ExitFacing);
            Vector3 lookDir = new Vector3(dirCoords.x, 0, dirCoords.z);
            if (lookDir != Vector3.zero)
            {
                portalObj.transform.rotation = Quaternion.LookRotation(lookDir);
            }
        }
    }

    public void DamageWallAt(GridCoords coords, Direction dir, float damage)
    {
        Tile tile = _mapManager.GetTile(coords);
        if (tile == null) return;

        RuntimeEdge edge = tile.GetEdge(dir);
        if (edge != null && edge.Type != EdgeType.Open)
        {
            edge.TakeDamage(damage);
        }
    }

    public void DamagePillarAt(GridCoords coords, float damage)
    {
        Tile tile = _mapManager.GetTile(coords);
        if (tile == null) return;

        var occupants = tile.Occupants;
        for (int i = 0; i < occupants.Count; i++)
        {
            if (occupants[i] is PillarInfo pillar)
            {
                pillar.TakeDamage(damage);
                break;
            }
        }
    }
}