using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class Unit : MonoBehaviour, ITileOccupant
{
    // [Fix] Faction 프로퍼티 (MapEnums의 Faction 사용)
    public Faction Faction { get; private set; }

    // [Fix] 좌표 프로퍼티 통합
    public GridCoords Coordinate { get; private set; }
    public GridCoords Coords => Coordinate;

    public UnitDataSO Data { get; private set; }

    public int CurrentHP { get; private set; }
    public int CurrentAP { get; private set; }
    public int MaxAP { get; private set; }
    public int CurrentMobility { get; private set; }
    public bool HasStartedMoving { get; private set; }
    public int Mobility => Data != null ? Data.Mobility : 5;

    private IUnitController _controller;
    public IUnitController Controller => _controller;

    private Collider _collider;
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _rotateSpeed = 15.0f;

    private bool _isSpawned = false;

    // ========================================================================
    // ITileOccupant 구현
    // ========================================================================
    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => true;
    public bool IsCover => false;

    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;

    public event Action<Unit> OnUnitInitialized;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public void Initialize(UnitDataSO data, Faction faction)
    {
        Data = data;
        Faction = faction;
        gameObject.name = Data != null ? $"{Data.UnitName}" : "Unit";

        if (Data != null)
        {
            CurrentHP = Data.MaxHP;
            MaxAP = Data.MaxAP;
            CurrentAP = MaxAP;
        }
        else
        {
            MaxAP = 2;
            CurrentAP = 2;
        }

        CurrentMobility = Mobility;
        HasStartedMoving = false;

        // 시각적 디버깅
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (faction == Faction.Player) renderer.material.color = Color.blue;
            else if (faction == Faction.Enemy) renderer.material.color = Color.red;
            else renderer.material.color = Color.green;
        }

        OnUnitInitialized?.Invoke(this);
    }

    // [Fix] 비주얼 위치 설정만 담당
    public void SpawnOnMap(GridCoords coords)
    {
        if (!ServiceLocator.TryGet(out MapManager mapManager))
            throw new InvalidOperationException($"[{nameof(Unit)}] MapManager not found.");

        Tile tile = mapManager.GetTile(coords);
        if (tile == null)
            throw new ArgumentException($"[{nameof(Unit)}] Invalid Spawn Coords: {coords}");

        Coordinate = coords;
        _isSpawned = true;

        Vector3 worldPos = GridUtils.GridToWorld(coords);
        float targetY = GetTargetHeight(worldPos.y);
        transform.position = new Vector3(worldPos.x, targetY, worldPos.z);

        if (Data != null)
            gameObject.name = $"{Data.UnitName}_{Coordinate} ({Faction})";
    }

    // [Fix] CS0535 해결: 인터페이스 메서드 구현
    public void OnAddedToTile(Tile tile)
    {
        Coordinate = tile.Coordinate;
        // 필요시 추가 로직
    }

    // [Fix] CS0535 해결: 인터페이스 메서드 구현
    public void OnRemovedFromTile(Tile tile)
    {
        // 필요시 추가 로직
    }

    public void SetController(IUnitController controller)
    {
        if (_controller != null) _controller.Unpossess();
        _controller = controller;
        if (_controller != null) _controller.Possess(this);
    }

    public async UniTask OnTurnStart()
    {
        ResetAP();
        if (_controller != null) await _controller.OnTurnStart();
    }

    public void OnTurnEnd() => _controller?.OnTurnEnd();

    public async UniTask MovePathAsync(List<GridCoords> path, MapManager mapManager)
    {
        if (path == null || path.Count == 0) return;

        if (!HasStartedMoving)
        {
            if (CurrentAP < 1) return;
            ConsumeAP(1);
            HasStartedMoving = true;
        }

        int distance = path.Count;
        CurrentMobility = Mathf.Max(0, CurrentMobility - distance);

        foreach (var nextCoords in path)
        {
            Tile nextTile = mapManager.GetTile(nextCoords);
            Tile currentTile = mapManager.GetTile(Coordinate);

            if (currentTile != null) currentTile.RemoveOccupant(this);
            if (nextTile != null) nextTile.AddOccupant(this);

            Coordinate = nextCoords;

            Vector3 targetPos = GridUtils.GridToWorld(nextCoords);
            targetPos.y = GetTargetHeight(targetPos.y);

            Vector3 direction = (targetPos - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                RotateTo(targetRot).Forget();
            }

            while (Vector3.Distance(transform.position, targetPos) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPos, _moveSpeed * Time.deltaTime);
                await UniTask.Yield();
            }
            transform.position = targetPos;
        }
    }

    private async UniTaskVoid RotateTo(Quaternion targetRot)
    {
        float t = 0;
        Quaternion startRot = transform.rotation;
        while (t < 1f)
        {
            t += Time.deltaTime * _rotateSpeed;
            transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            await UniTask.Yield();
        }
        transform.rotation = targetRot;
    }

    public void ConsumeAP(int amount) => CurrentAP = Mathf.Max(0, CurrentAP - amount);

    public void ResetAP()
    {
        CurrentAP = MaxAP > 0 ? MaxAP : 2;
        CurrentMobility = Mobility;
        HasStartedMoving = false;
    }

    public void TakeDamage(int damage)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        if (CurrentHP <= 0) Destroy(gameObject);
    }

    private float GetTargetHeight(float surfaceY)
    {
        if (_collider == null) return surfaceY;
        return surfaceY + _collider.bounds.extents.y;
    }

    private void OnDestroy()
    {
        if (!_isSpawned) return;

        if (ServiceLocator.TryGet(out MapManager mapManager))
        {
            Tile tile = mapManager.GetTile(Coordinate);
            if (tile != null) tile.RemoveOccupant(this);
        }
        if (ServiceLocator.TryGet(out UnitManager unitManager))
        {
            unitManager.UnregisterUnit(this);
        }
    }
}