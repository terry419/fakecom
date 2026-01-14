using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

[DependsOn(typeof(MapManager), typeof(TileDataManager))]
public class EnvironmentManager : MonoBehaviour, IInitializable
{
    private MapManager _mapManager;
    private TileDataManager _tileDataManager;
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

        // [New] 빌드 결과 검증
        ValidateBuildResult();

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

    // [Fix] 4방향 모두 처리 (데이터 누락 방지)
    private void LinkEdgesForTile(Tile tile)
    {
        GridCoords coords = tile.Coordinate;

        // North <-> Neighbor South
        ProcessSharedEdge(tile, Direction.North, new GridCoords(coords.x, coords.z + 1, coords.y), Direction.South);
        // East <-> Neighbor West
        ProcessSharedEdge(tile, Direction.East, new GridCoords(coords.x + 1, coords.z, coords.y), Direction.West);
        // South <-> Neighbor North (추가됨)
        ProcessSharedEdge(tile, Direction.South, new GridCoords(coords.x, coords.z - 1, coords.y), Direction.North);
        // West <-> Neighbor East (추가됨)
        ProcessSharedEdge(tile, Direction.West, new GridCoords(coords.x - 1, coords.z, coords.y), Direction.East);
    }

    private void ProcessSharedEdge(Tile currentTile, Direction currentDir, GridCoords neighborPos, Direction neighborDir)
    {
        Tile neighbor = _mapManager.GetTile(neighborPos);

        // 1. 내 쪽 데이터
        SavedEdgeInfo myInfo = (currentTile.TempSavedEdges != null)
            ? currentTile.TempSavedEdges[(int)currentDir]
            : GetDefaultOpenEdge(); // 안전장치

        // 2. 이웃 쪽 데이터 (TempSavedEdges null 체크 추가)
        SavedEdgeInfo neighborInfo = (neighbor != null && neighbor.TempSavedEdges != null)
            ? neighbor.TempSavedEdges[(int)neighborDir]
            : GetDefaultOpenEdge();

        // [Log] 데이터 불일치 경고
        if (neighbor != null && myInfo.Type != neighborInfo.Type)
        {
            // Open vs Wall 같은 의도된 불일치는 제외하고, Wall vs Window 같이 애매한 경우만 경고할 수도 있음
            // 현재는 디버깅을 위해 로그 남김 (너무 많으면 주석 처리)
            // Debug.LogWarning($"[Edge Merge] {currentTile.Coordinate}({currentDir})={myInfo.Type} vs {neighbor.Coordinate}({neighborDir})={neighborInfo.Type}");
        }

        // 3. 데이터 병합 (우선순위 기반)
        SavedEdgeInfo finalInfo = MergeEdgeInfo(myInfo, neighborInfo);

        // 4. 런타임 엣지 생성
        // [Fix CS0019] '!=' 연산자 에러 수정. EdgeEntry가 struct이므로 null 체크 제거
        var edgeDataEntry = _tileDataManager.GetEdgeData(finalInfo.Type);
        bool isPassable = edgeDataEntry.IsPassable;

        RuntimeEdge edge = new RuntimeEdge(finalInfo.Type, finalInfo.Cover, finalInfo.MaxHP, finalInfo.CurrentHP, isPassable);

        // 5. 공유 (양쪽 타일에 동일 객체 할당)
        currentTile.SetSharedEdge(currentDir, edge);
        if (neighbor != null)
        {
            neighbor.SetSharedEdge(neighborDir, edge);
        }
    }

    // [Logic] 우선순위 기반 병합 (Wall > Door > Window > Fence > Open)
    private SavedEdgeInfo MergeEdgeInfo(SavedEdgeInfo a, SavedEdgeInfo b)
    {
        int priorityA = GetEdgePriority(a.Type);
        int priorityB = GetEdgePriority(b.Type);

        SavedEdgeInfo stronger = (priorityA >= priorityB) ? a : b;

        // 세부 속성 병합 (Cover는 더 높은 것, HP는 최대값)
        CoverType finalCover = (a.Cover > b.Cover) ? a.Cover : b.Cover;
        float finalMaxHP = Mathf.Max(a.MaxHP, b.MaxHP);
        float finalCurrentHP = Mathf.Max(a.CurrentHP, b.CurrentHP);

        // 구조체 반환 (SavedEdgeInfo 생성자가 있다고 가정)
        // 만약 생성자가 없다면 stronger를 기반으로 값만 수정해서 반환해야 함
        return new SavedEdgeInfo(stronger.Type, finalCover, finalMaxHP, finalCurrentHP);
    }

    private int GetEdgePriority(EdgeType type)
    {
        return type switch
        {
            EdgeType.Wall => 5,
            EdgeType.Door => 4,
            EdgeType.Window => 3,
            EdgeType.Fence => 2,
            EdgeType.Open => 1,
            _ => 0
        };
    }

    // [Helper] SavedEdgeInfo.CreateOpen()이 없을 경우를 대비한 로컬 팩토리
    private SavedEdgeInfo GetDefaultOpenEdge()
    {
        // SavedEdgeInfo에 생성자가 (Type, Cover, MaxHP, CurHP) 순서라고 가정
        return new SavedEdgeInfo(EdgeType.Open, CoverType.None, 0f, 0f);
    }

    private void ProcessPortal(Tile tile)
    {
        if (tile.PortalData == null) return;

        GameObject prefab = _tileDataManager.GetPortalPrefab(tile.PortalData.Type);
        if (prefab == null)
        {
            Debug.LogWarning($"[EnvironmentManager] Portal prefab missing for {tile.PortalData.Type}");
            return;
        }

        try
        {
            Vector3 worldPos = GridUtils.GridToWorld(tile.Coordinate);
            GameObject portalObj = Instantiate(prefab, worldPos, Quaternion.identity);

            portalObj.transform.SetParent(_portalParent);
            portalObj.name = $"Portal_{tile.PortalData.Type}_{tile.Coordinate}";

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
        catch (System.Exception ex)
        {
            Debug.LogError($"[EnvironmentManager] Failed to create portal at {tile.Coordinate}: {ex.Message}");
        }
    }

    // [New] 빌드 결과 검증 로직
    private void ValidateBuildResult()
    {
        int edgeErrors = 0;
        foreach (Tile tile in _mapManager.GetAllTiles())
        {
            for (int i = 0; i < 4; i++)
            {
                RuntimeEdge edge = tile.GetEdge((Direction)i);
                if (edge == null)
                {
                    // 심각한 오류: 모든 타일은 4방향 엣지를 가져야 함 (Open이라도)
                    Debug.LogWarning($"[EnvironmentManager] Tile {tile.Coordinate} missing edge at {(Direction)i}");
                    edgeErrors++;
                }
            }
        }

        if (edgeErrors > 0)
        {
            Debug.LogError($"[EnvironmentManager] Found {edgeErrors} edge validation errors! Check MapData or Logic.");
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