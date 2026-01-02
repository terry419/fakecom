using UnityEngine;
using Cysharp.Threading.Tasks;

[DependsOn(typeof(MapManager), typeof(TileDataManager))]
public class EnvironmentManager : MonoBehaviour, IInitializable
{
    private MapManager _mapManager;
    private TileDataManager _tileDataManager;

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

        // MapManager의 모든 타일 순회 (전수 조사)
        foreach (Tile tile in _mapManager.GetAllTiles())
        {
            // 1. 기둥(Pillar) 처리
            ProcessPillar(tile);

            // 2. 벽/창문 처리 (North, East 방향만 처리하여 중복 생성 방지)
            LinkEdgesForTile(tile);
        }

        Debug.Log("[EnvironmentManager] Build Complete.");
    }

    private void ProcessPillar(Tile tile)
    {
        if (tile.InitialPillarID == PillarType.None) return;

        // [Fix CS0019] 데이터 유무 확인 및 null 체크 수행
        var pillarData = _tileDataManager.GetPillarData(tile.InitialPillarID);

        // 유효성 체크 (Prefab 존재 여부 확인)
        if (pillarData.Prefab == null)
        {
            Debug.LogError($"[EnvManager] Pillar Data missing/invalid for ID: {tile.InitialPillarID}");
            return;
        }

        // 특정 좌표 디버깅: (18, 6, 0) 타일 로드 확인용
        if (tile.Coordinate.x == 18 && tile.Coordinate.z == 6)
        {
            Debug.Log($"<color=green>[EnvManager] Creating Pillar at {tile.Coordinate}: {tile.InitialPillarID}</color>");
        }

        var pillarInfo = new PillarInfo(tile.InitialPillarID, pillarData.MaxHP, pillarData.MaxHP);
        tile.AddOccupant(pillarInfo);
    }

    private void LinkEdgesForTile(Tile currentTile)
    {
        GridCoords pos = currentTile.Coordinate;

        // [Fix] GridCoords 좌표계 (x, z, y) 해석 기준 적용

        // 1. North Check (Z + 1 방향 확인)
        // 2번째 인자가 z이므로 여기에 pos.z + 1 대입
        GridCoords northPos = new GridCoords(pos.x, pos.z + 1, pos.y);

        if (_mapManager.HasTile(northPos))
        {
            ProcessEdge(currentTile, Direction.North, northPos, Direction.South);
        }

        // 2. East Check (X + 1 방향 확인)
        // 1번째 인자가 x이므로 여기에 pos.x + 1 대입
        GridCoords eastPos = new GridCoords(pos.x + 1, pos.z, pos.y);

        if (_mapManager.HasTile(eastPos))
        {
            ProcessEdge(currentTile, Direction.East, eastPos, Direction.West);
        }
    }

    private void ProcessEdge(Tile currentTile, Direction currentDir, GridCoords neighborPos, Direction neighborDir)
    {
        // 현재 타일의 저장된 Edge 정보 가져오기
        SavedEdgeInfo info = currentTile.TempSavedEdges[(int)currentDir];

        // 런타임용 Edge 객체 생성
        RuntimeEdge edge = new RuntimeEdge(info.Type, info.Cover, info.MaxHP, info.CurrentHP);

        // 현재 타일에 설정
        currentTile.SetSharedEdge(currentDir, edge);

        // 이웃 타일에도 동일한 Edge 공유 설정 (데이터 동기화)
        Tile neighbor = _mapManager.GetTile(neighborPos);
        if (neighbor != null)
        {
            neighbor.SetSharedEdge(neighborDir, edge);
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
            // 필요 시 파괴 효과나 사운드 추가 가능
        }
    }
    public void DamagePillarAt(GridCoords coords, float damage)
    {
        Tile tile = _mapManager.GetTile(coords);
        if (tile == null) return;

        // 타일의 점유자들 중에서 PillarInfo를 찾아서 데미지 적용
        foreach (var occupant in tile.Occupants)
        {
            if (occupant is PillarInfo pillar)
            {
                pillar.TakeDamage(damage);
                // 기둥은 타일당 하나라고 가정하고 찾으면 즉시 종료
                break;
            }
        }
    }
}