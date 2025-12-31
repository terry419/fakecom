using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
{
    private const int FRAME_TIME_BUDGET_MS = 16;
    private Tile[,,] _tiles;

    private int _gridWidth;
    private int _gridDepth;
    private int _layerCount;
    private int _minLevel;

    private bool _isInitialized = false;

    // Getters
    public int GridWidth => _gridWidth;
    public int GridDepth => _gridDepth;
    public int LayerCount => _layerCount;
    public int MinLevel => _minLevel;

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
        if (mapData == null) throw new ArgumentNullException(nameof(mapData));

        try
        {
            // [핵심] TileDataManager를 통해 레지스트리 정보에 접근
            // (TileDataManager가 없다면 여기서 에러가 발생하여 문제를 바로 알 수 있습니다)
            var tileMgr = ServiceLocator.Get<TileDataManager>();

            int declaredWidth = mapData.GridSize.x;
            int declaredDepth = mapData.GridSize.y;
            _minLevel = mapData.MinLevel;
            _layerCount = mapData.MaxLevel - mapData.MinLevel + 1;

            // 그리드 크기 보정 로직
            int maxX = declaredWidth - 1;
            int maxZ = declaredDepth - 1;
            foreach (var tileData in mapData.Tiles)
            {
                maxX = Mathf.Max(maxX, tileData.Coords.x);
                maxZ = Mathf.Max(maxZ, tileData.Coords.z);
            }
            _gridWidth = maxX + 1;
            _gridDepth = maxZ + 1;

            _tiles = new Tile[_gridWidth, _gridDepth, _layerCount];

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            foreach (var tileData in mapData.Tiles)
            {
                if (stopwatch.ElapsedMilliseconds > FRAME_TIME_BUDGET_MS)
                {
                    await UniTask.Yield();
                    stopwatch.Restart();
                }

                int x = tileData.Coords.x;
                int z = tileData.Coords.z;
                int levelIndex = tileData.Coords.y - _minLevel;

                // 1. 타일 객체 생성
                Tile tile = new Tile(tileData.Coords, tileData.FloorID, tileData.PillarID);

                // 2. 저장된 데이터(Edge 등) 로드
                tile.LoadFromSaveData(tileData);

                // 3. [데이터 주입] 레지스트리에서 기둥의 MaxHP 등을 가져와 설정
                if (tileData.PillarID != PillarType.None)
                {
                    var pillarEntry = tileMgr.GetPillarData(tileData.PillarID);

                    // 저장된 HP가 있다면(>0) 그것을 쓰고, 없다면(-1 or 0) MaxHP로 초기화
                    // (새로 만든 맵은 CurrentPillarHP가 0일 수 있으므로 MaxHP를 기본값으로 사용)
                    float currentHP = tileData.CurrentPillarHP > 0 ? tileData.CurrentPillarHP : pillarEntry.MaxHP;

                    tile.InitializePillarHP(pillarEntry.MaxHP, currentHP);
                }

                _tiles[x, z, levelIndex] = tile;
            }

            Debug.Log($"[MapManager] Map '{mapData.DisplayName}' Loaded. Size: {_gridWidth}x{_gridDepth}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MapManager] LoadMap failed: {ex.Message}");
            throw;
        }
    }

    public Tile GetTile(GridCoords coords)
    {
        if (!_isInitialized) return null;
        int levelIndex = coords.y - _minLevel;
        if (IsOutOfBounds(coords.x, coords.z, levelIndex)) return null;
        return _tiles[coords.x, coords.z, levelIndex];
    }

    private bool IsOutOfBounds(int x, int z, int levelIndex)
    {
        return x < 0 || x >= _gridWidth || z < 0 || z >= _gridDepth || levelIndex < 0 || levelIndex >= _layerCount;
    }
}