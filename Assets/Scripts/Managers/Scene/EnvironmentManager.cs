using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class EnvironmentManager : MonoBehaviour, IInitializable
{
    private MapManager _mapManager;
    private TileDataManager _tileDataManager;

    // 1. 등록 (ServiceLocator)
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy() => ServiceLocator.Unregister<EnvironmentManager>(ManagerScope.Scene);

    // 2. 초기화
    public async UniTask Initialize(InitializationContext context)
    {
        _mapManager = ServiceLocator.Get<MapManager>();
        _tileDataManager = ServiceLocator.Get<TileDataManager>();
        await UniTask.CompletedTask;
    }

    // [3단계 핵심] MapManager가 로딩 끝난 직후 호출함
    public void BuildMapFeatures()
    {
        if (_mapManager == null) return;

        Debug.Log("[EnvironmentManager] Start Building Features (Pillars & Edges)...");

        int width = _mapManager.GridWidth;
        int depth = _mapManager.GridDepth;
        int layers = _mapManager.LayerCount;

        // 전체 타일 순회
        for (int y = 0; y < layers; y++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    GridCoords coords = new GridCoords(x, z, _mapManager.MinLevel + y);
                    Tile tile = _mapManager.GetTile(coords);
                    if (tile == null) continue;

                    // 1. 기둥 승격 (Pillar Promotion)
                    ProcessPillar(tile);

                    // 2. 엣지 연결 (Link Edges - SSOT Wiring)
                    LinkEdgesForTile(tile, x, z, y);
                }
            }
        }

        Debug.Log("[EnvironmentManager] Build Complete.");
    }

    // --- 기둥 처리 로직 ---
    private void ProcessPillar(Tile tile)
    {
        if (tile.InitialPillarID == PillarType.None) return;

        // 데이터 매니저에서 기둥 스펙(MaxHP 등) 가져오기
        var pillarData = _tileDataManager.GetPillarData(tile.InitialPillarID);
        if (pillarData.Prefab == null) return;

        // 논리 객체(PillarInfo) 생성 및 타일 점유
        // (현재 HP 로직이 따로 없다면 MaxHP로 초기화)
        var pillarInfo = new PillarInfo(tile.InitialPillarID, pillarData.MaxHP, pillarData.MaxHP);

        tile.AddOccupant(pillarInfo);
    }

    // --- 엣지 연결 로직 (단일 소스 원칙) ---
    // 알고리즘: 모든 타일은 자신의 "북쪽(North)"과 "동쪽(East)" 벽만 생성해서 책임진다.
    // 내 북쪽 벽 = 북쪽 이웃의 남쪽 벽 (공유)
    // 내 동쪽 벽 = 동쪽 이웃의 서쪽 벽 (공유)
    private void LinkEdgesForTile(Tile currentTile, int x, int z, int y)
    {
        // 1. North 처리
        // 내 TempData에서 North 정보 가져와서 RuntimeEdge 생성
        SavedEdgeInfo northInfo = currentTile.TempSavedEdges[(int)Direction.North];
        RuntimeEdge northEdge = new RuntimeEdge(northInfo.Type, northInfo.Cover, northInfo.MaxHP, northInfo.CurrentHP);

        // 나한테 꽂기
        currentTile.SetSharedEdge(Direction.North, northEdge);

        // 북쪽 이웃 찾아서 꽂기 (이웃의 South는 나와 같은 객체)
        Tile northNeighbor = _mapManager.GetTile(new GridCoords(x, z + 1, _mapManager.MinLevel + y));
        if (northNeighbor != null)
        {
            northNeighbor.SetSharedEdge(Direction.South, northEdge);
        }

        // 2. East 처리
        SavedEdgeInfo eastInfo = currentTile.TempSavedEdges[(int)Direction.East];
        RuntimeEdge eastEdge = new RuntimeEdge(eastInfo.Type, eastInfo.Cover, eastInfo.MaxHP, eastInfo.CurrentHP);

        // 나한테 꽂기
        currentTile.SetSharedEdge(Direction.East, eastEdge);

        // 동쪽 이웃 찾아서 꽂기 (이웃의 West는 나와 같은 객체)
        Tile eastNeighbor = _mapManager.GetTile(new GridCoords(x + 1, z, _mapManager.MinLevel + y));
        if (eastNeighbor != null)
        {
            eastNeighbor.SetSharedEdge(Direction.West, eastEdge);
        }

        // 주의: South와 West는 처리하지 않음.
        // 왜냐하면 나의 South는 "내 남쪽 이웃"이 루프를 돌 때 그의 North로서 처리해서 나에게 꽂아주기 때문.
        // (단, 맵의 가장자리일 경우 null 상태일 수 있으므로, 예외적으로 맵 경계선 처리가 필요하다면 추가 로직 필요)
        // 여기서는 MapManager가 Load 시 이미 TempSavedEdges에 Open을 채워두므로,
        // 가장자리(이웃 없음)인 경우에도 자신의 Edge는 생성되어 할당됨.
    }

    // [4단계] 파괴 로직 (외부에서 호출)
    public void DamageWallAt(GridCoords coords, Direction dir, float damage)
    {
        Tile tile = _mapManager.GetTile(coords);
        if (tile == null) return;

        RuntimeEdge edge = tile.GetEdge(dir);
        if (edge == null || edge.Type == EdgeType.Open) return;

        // 데미지 적용 (공유 객체이므로 연결된 반대편 타일 데이터도 즉시 바뀜)
        edge.TakeDamage(damage);

        // 만약 파괴되었다면, 양쪽 타일의 캐시(이동 가능 여부)를 갱신해야 함
        if (edge.IsBroken)
        {
            // 1. 내 타일 갱신
            tile.UpdateCache();

            // 2. 반대편 이웃 타일 갱신
            GridCoords neighborCoords = GridUtils.GetNeighbor(coords, dir);
            Tile neighbor = _mapManager.GetTile(neighborCoords);
            if (neighbor != null)
            {
                neighbor.UpdateCache();
            }

            Debug.Log($"[EnvManager] Wall Destroyed at {coords} ({dir})");

            // TODO: 추후 여기에 비주얼 파괴 이벤트(Event) 호출 추가
        }
    }
}