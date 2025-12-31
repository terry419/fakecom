using Cysharp.Threading.Tasks;
using System.Linq; // Min, Max 계산을 위해 사용 (성능이 중요하다면 foreach로 변경 가능)
using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
{
    private const int FRAME_TIME_BUDGET_MS = 16;
    private Tile[,,] _tiles;

    // 맵 데이터 정보
    private int _gridWidth;
    private int _gridDepth;
    private int _layerCount;
    private int _minLevel;
    private int _maxLevel;

    // 계산된 오프셋
    private Vector2Int _basePosition;

    private bool _isInitialized = false;

    // Getters
    public int GridWidth => _gridWidth;
    public int GridDepth => _gridDepth;
    public int LayerCount => _layerCount;
    public int MinLevel => _minLevel;
    public Vector2Int BasePosition => _basePosition;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy()
    {
        ServiceLocator.Unregister<MapManager>(ManagerScope.Scene);
        _isInitialized = false;
    }

    public async UniTask Initialize(InitializationContext context)
    {
        if (context.MapData != null)
        {
            await LoadMap(context.MapData);
        }
        _isInitialized = true;
    }

    public async UniTask LoadMap(MapDataSO mapData)
    {
        if (mapData == null || mapData.Tiles == null || mapData.Tiles.Count == 0)
        {
            Debug.LogError("[MapManager] MapData is null or empty!");
            return;
        }

        // [핵심 변경] GridSize 메타데이터를 신뢰하지 않고, 실제 데이터로 범위 계산
        CalculateActualBounds(mapData, out var minPos, out var size);

        _basePosition = minPos;
        _gridWidth = size.x;
        _gridDepth = size.y;

        _minLevel = mapData.MinLevel;
        _maxLevel = mapData.MaxLevel;
        _layerCount = _maxLevel - _minLevel + 1;

        Debug.Log($"[MapManager] Bounds Calculated: Base{_basePosition}, Size({_gridWidth}x{_gridDepth})");

        // 2. 타일 배열 초기화
        _tiles = new Tile[_gridWidth, _gridDepth, _layerCount];

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 3. 타일 데이터 로드
        foreach (var tileData in mapData.Tiles)
        {
            if (stopwatch.ElapsedMilliseconds > FRAME_TIME_BUDGET_MS)
            {
                await UniTask.Yield();
                stopwatch.Restart();
            }

            // [좌표 변환] World -> Local Array Index
            int x = tileData.Coords.x - _basePosition.x;
            int z = tileData.Coords.z - _basePosition.y;
            int levelIndex = tileData.Coords.y - _minLevel;

            // 로직상 범위 밖일 수가 없으나(위에서 계산했으므로), 방어 코드 유지
            if (IsOutOfBoundsLocal(x, z, levelIndex))
            {
                Debug.LogWarning($"[MapManager] Unexpected Out-of-Bounds: {tileData.Coords}");
                continue;
            }

            Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
            tile.LoadFromSaveData(tileData);
            _tiles[x, z, levelIndex] = tile;
        }

        // [3단계] 환경 구축
        var envManager = ServiceLocator.Get<EnvironmentManager>();
        if (envManager != null) envManager.BuildMapFeatures();

        Debug.Log($"[MapManager] Map Loaded Successfully: {mapData.name}");
    }

    // 실제 데이터 경계 계산 함수
    private void CalculateActualBounds(MapDataSO data, out Vector2Int minPos, out Vector2Int size)
    {
        int minX = int.MaxValue;
        int minZ = int.MaxValue;
        int maxX = int.MinValue;
        int maxZ = int.MinValue;

        foreach (var t in data.Tiles)
        {
            if (t.Coords.x < minX) minX = t.Coords.x;
            if (t.Coords.z < minZ) minZ = t.Coords.z;
            if (t.Coords.x > maxX) maxX = t.Coords.x;
            if (t.Coords.z > maxZ) maxZ = t.Coords.z;
        }

        minPos = new Vector2Int(minX, minZ);
        // (Max - Min + 1)이 실제 크기
        size = new Vector2Int(maxX - minX + 1, maxZ - minZ + 1);
    }

    public Tile GetTile(GridCoords coords)
    {
        if (!_isInitialized) return null;
        int localX = coords.x - _basePosition.x;
        int localZ = coords.z - _basePosition.y;
        int levelIndex = coords.y - _minLevel;

        if (IsOutOfBoundsLocal(localX, localZ, levelIndex)) return null;
        return _tiles[localX, localZ, levelIndex];
    }

    private bool IsOutOfBoundsLocal(int localX, int localZ, int levelIndex)
    {
        return localX < 0 || localX >= _gridWidth ||
               localZ < 0 || localZ >= _gridDepth ||
               levelIndex < 0 || levelIndex >= _layerCount;
    }
}