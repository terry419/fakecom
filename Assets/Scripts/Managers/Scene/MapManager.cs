using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
{
    // [리팩토링 9] 매직넘버 상수화 (P3)
    private const int FRAME_TIME_BUDGET_MS = 16;

    // GDD 5.6: 3차원 배열 [Width(x), Depth(z), Height(y)]
    private Tile[,,] _tiles;

    // [리팩토링 1] 좌표계 변수명 명확화 (P0)
    // Vector3Int _mapDimensions 대신 명시적인 변수 사용으로 혼동 원천 차단
    private int _gridWidth;         // x축 (가로)
    private int _gridDepth;         // z축 (세로/깊이) - 기존의 y와 헷갈리던 부분
    private int _layerCount;        // y축 (층수/높이)
    private int _minLevel;          // 최저 층 높이 보정값

    // [리팩토링 3] 초기화 안전장치 플래그
    private bool _isInitialized = false;

    // [리팩토링 8] 캡슐화: 외부에서는 읽기만 가능 (P2)
    public int GridWidth => _gridWidth;
    public int GridDepth => _gridDepth;
    public int LayerCount => _layerCount;
    public int MinLevel => _minLevel;

    // ServiceLocator 등록은 Awake에서 유지 (BootManager가 찾을 수 있도록)
    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MapManager>(ManagerScope.Scene);
        _isInitialized = false;
    }

    // [리팩토링 2] InitializationContext를 통한 데이터 로드
    public async UniTask Initialize(InitializationContext context)
    {
        // 데이터가 있으면 즉시 로드, 없으면 대기 (유연한 구조)
        if (context.MapData != null)
        {
            await LoadMap(context.MapData);
        }
        _isInitialized = true;
    }

    public async UniTask LoadMap(MapDataSO mapData)
    {
        // [리팩토링 7] 필수 데이터 검증 강화 (P1)
        if (mapData == null)
            throw new ArgumentNullException(nameof(mapData), "[MapManager] MapDataSO is null.");

        try
        {
            Debug.Log($"[MapManager] Loading Map: {mapData.DisplayName}");

            // [리팩토링 1] 명확한 변수 매핑
            // MapData.GridSize.x -> Width (가로)
            // MapData.GridSize.y -> Depth (세로/깊이) *가장 헷갈리던 부분 해결*
            _gridWidth = mapData.GridSize.x;
            _gridDepth = mapData.GridSize.y;
            _layerCount = mapData.MaxLevel - mapData.MinLevel + 1;
            _minLevel = mapData.MinLevel;

            _tiles = new Tile[_gridWidth, _gridDepth, _layerCount];

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int loadedCount = 0;

            foreach (var tileData in mapData.Tiles)
            {
                // [리팩토링 9] 16ms 제한 로직
                if (stopwatch.ElapsedMilliseconds > FRAME_TIME_BUDGET_MS)
                {
                    await UniTask.Yield();
                    stopwatch.Restart();
                }

                int x = tileData.Coords.x;
                int z = tileData.Coords.z;
                // 로직상 높이는 levelIndex로 변환하여 사용
                int levelIndex = tileData.Coords.y - _minLevel;

                if (IsOutOfBounds(x, z, levelIndex))
                {
                    continue; // 범위 밖 타일 무시
                }

                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);
                tile.LoadFromSaveData(tileData);

                _tiles[x, z, levelIndex] = tile;
                loadedCount++;
            }

            Debug.Log($"[MapManager] Map Loaded. Tiles: {loadedCount}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MapManager] LoadMap Failed: {ex.Message}");
            throw; // 에러를 상위로 전파하여 부팅 중단
        }
    }

    public Tile GetTile(GridCoords coords)
    {
        // [리팩토링 3] 초기화 전 접근 차단 (안전장치)
        if (!_isInitialized)
        {
            Debug.LogError("[MapManager] Not initialized yet.");
            return null;
        }

        int levelIndex = coords.y - _minLevel;
        if (IsOutOfBounds(coords.x, coords.z, levelIndex)) return null;

        return _tiles[coords.x, coords.z, levelIndex];
    }

    // [리팩토링 1] 명확한 변수명을 사용한 경계 검사
    private bool IsOutOfBounds(int x, int z, int levelIndex)
    {
        return x < 0 || x >= _gridWidth ||              // x축 검사
               z < 0 || z >= _gridDepth ||              // z축 검사 (기존 y와 혼동 해결)
               levelIndex < 0 || levelIndex >= _layerCount; // y축(층) 검사
    }
}