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

        // MapManager의 모든 타일을 직접 순회 (좌표 계산 로직 제거)
        foreach (Tile tile in _mapManager.GetAllTiles())
        {
            // 1. 기둥 생성
            ProcessPillar(tile);

            // 2. 벽/창문 연결 (North, East 방향만 처리하여 중복 방지)
            LinkEdgesForTile(tile);
        }

        Debug.Log("[EnvironmentManager] Build Complete.");
    }

    private void ProcessPillar(Tile tile)
    {
        if (tile.InitialPillarID == PillarType.None) return;

        // [Fix CS0019] 구조체는 null이 될 수 없으므로 null 체크 제거
        var pillarData = _tileDataManager.GetPillarData(tile.InitialPillarID);

        // 데이터 무결성 체크 (Prefab 존재 여부 확인)
        if (pillarData.Prefab == null)
        {
            Debug.LogError($"[EnvManager] Pillar Data missing/invalid for ID: {tile.InitialPillarID}");
            return;
        }

        // 디버그: (18, 6, 0) 타일 확인용
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

        // [Fix] GridCoords 생성자 순서 (x, z, y) 준수

        // 1. North Check (Z + 1)
        // 생성자 2번째 인자가 z이므로 여기에 pos.z + 1 전달
        GridCoords northPos = new GridCoords(pos.x, pos.z + 1, pos.y);

        if (_mapManager.HasTile(northPos))
        {
            ProcessEdge(currentTile, Direction.North, northPos, Direction.South);
        }

        // 2. East Check (X + 1)
        // 생성자 1번째 인자가 x이므로 여기에 pos.x + 1 전달
        GridCoords eastPos = new GridCoords(pos.x + 1, pos.z, pos.y);

        if (_mapManager.HasTile(eastPos))
        {
            ProcessEdge(currentTile, Direction.East, eastPos, Direction.West);
        }
    }

    private void ProcessEdge(Tile currentTile, Direction currentDir, GridCoords neighborPos, Direction neighborDir)
    {
        // 내 타일의 Edge 정보 가져오기
        SavedEdgeInfo info = currentTile.TempSavedEdges[(int)currentDir];

        // 런타임 객체 생성
        RuntimeEdge edge = new RuntimeEdge(info.Type, info.Cover, info.MaxHP, info.CurrentHP);

        // 나에게 설정
        currentTile.SetSharedEdge(currentDir, edge);

        // 이웃에게 설정 (공유)
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
            // 필요 시 시각적 갱신 로직 추가
        }
    }
}