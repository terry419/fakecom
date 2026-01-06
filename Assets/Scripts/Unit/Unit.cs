using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class Unit : MonoBehaviour, ITileOccupant
{
    // ... (상단 프로퍼티 유지) ...
    public Faction Faction { get; private set; }
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

    // ITileOccupant 구현
    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => true;
    public bool IsCover => false;

    // [Fix] 미사용 이벤트 경고 억제 (인터페이스 요구사항)
#pragma warning disable 67
    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;
#pragma warning restore 67

    public event Action<Unit> OnUnitInitialized;

    // ... (Initialize, SpawnOnMap, MovePathAsync 등 기존 로직 유지) ...

    private void Awake() => _collider = GetComponent<Collider>();

    public void Initialize(UnitDataSO data, Faction faction)
    {
        Data = data;
        Faction = faction;
        gameObject.name = Data != null ? $"{Data.UnitName}" : "Unit";
        if (Data != null) { CurrentHP = Data.MaxHP; MaxAP = Data.MaxAP; CurrentAP = MaxAP; }
        else { MaxAP = 2; CurrentAP = 2; }
        CurrentMobility = Mobility;
        HasStartedMoving = false;

        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (faction == Faction.Player) renderer.material.color = Color.blue;
            else if (faction == Faction.Enemy) renderer.material.color = Color.red;
            else renderer.material.color = Color.green;
        }
        OnUnitInitialized?.Invoke(this);
    }

    public void SpawnOnMap(GridCoords coords)
    {
        if (!ServiceLocator.TryGet(out MapManager mapManager))
            throw new InvalidOperationException($"[{nameof(Unit)}] MapManager not found.");
        Tile tile = mapManager.GetTile(coords);
        if (tile == null) throw new ArgumentException($"[{nameof(Unit)}] Invalid Spawn Coords: {coords}");

        Coordinate = coords;
        _isSpawned = true;

        Vector3 worldPos = GridUtils.GridToWorld(coords);
        float targetY = GetTargetHeight(worldPos.y);
        transform.position = new Vector3(worldPos.x, targetY, worldPos.z);

        if (Data != null) gameObject.name = $"{Data.UnitName}_{Coordinate} ({Faction})";
    }

    public void OnAddedToTile(Tile tile) => Coordinate = tile.Coordinate;
    public void OnRemovedFromTile(Tile tile) { }

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
        if (!HasStartedMoving) { if (CurrentAP < 1) return; ConsumeAP(1); HasStartedMoving = true; }

        int distance = path.Count;
        CurrentMobility = Mathf.Max(0, CurrentMobility - distance);

        foreach (var nextCoords in path)
        {
            mapManager.MoveUnit(this, nextCoords);
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
        float t = 0; Quaternion startRot = transform.rotation;
        while (t < 1f) { t += Time.deltaTime * _rotateSpeed; transform.rotation = Quaternion.Slerp(startRot, targetRot, t); await UniTask.Yield(); }
        transform.rotation = targetRot;
    }

    public void ConsumeAP(int amount) => CurrentAP = Mathf.Max(0, CurrentAP - amount);
    public void ResetAP() { CurrentAP = MaxAP > 0 ? MaxAP : 2; CurrentMobility = Mobility; HasStartedMoving = false; }
    public async UniTask TakeDamage(int damage)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - damage);

        // [연출 대기] 추후 애니메이션 길이에 맞춰 자동화 가능
        // 현재는 하드코딩된 시간 대신, 추후 Animation Clip Length 등을 사용할 수 있게 구조 마련
        await UniTask.Delay(500);

        if (CurrentHP <= 0)
        {
            // 사망 처리 (필요 시 비동기 사망 연출 추가 가능)
            Destroy(gameObject);
        }
    }
    private float GetTargetHeight(float surfaceY) => (_collider == null) ? surfaceY : surfaceY + _collider.bounds.extents.y;

    private void OnDestroy()
    {
        if (!_isSpawned) return;
        if (ServiceLocator.TryGet(out MapManager mapManager) && ServiceLocator.TryGet(out UnitManager unitManager))
        {
            mapManager.UnregisterUnit(this);
            unitManager.UnregisterUnit(this);
        }
    }
}