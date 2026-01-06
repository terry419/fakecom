using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

public class Unit : MonoBehaviour, ITileOccupant
{
    // ========================================================================
    // 1. 기본 정보 및 속성
    // ========================================================================
    public Faction Faction { get; private set; }
    public GridCoords Coordinate { get; private set; }
    public GridCoords Coords => Coordinate;
    public UnitDataSO Data { get; private set; }

    [Header("Class Settings")]
    public ClassType ClassType = ClassType.Assault; // 기본값

    // ========================================================================
    // 2. 스탯 및 상태
    // ========================================================================
    public int CurrentHP { get; private set; }

    public int CurrentMobility { get; private set; }
    public int Mobility => Data != null ? Data.Mobility : 5;

    public bool HasAttacked { get; private set; }
    public bool HasStartedMoving { get; private set; }

    // ========================================================================
    // 3. 컴포넌트 및 액션
    // ========================================================================
    private IUnitController _controller;
    public IUnitController Controller => _controller;

    private Collider _collider;
    [SerializeField] private float _moveSpeed = 3.0f;
    [SerializeField] private float _rotateSpeed = 15.0f;
    private bool _isSpawned = false;

    private List<BaseAction> _actions = new List<BaseAction>();
    private MoveAction _moveAction;
    private AttackAction _attackAction;

    public IReadOnlyList<BaseAction> GetActions() => _actions.AsReadOnly();
    public MoveAction GetMoveAction() => _moveAction;
    public AttackAction GetAttackAction() => _attackAction;
    public BaseAction GetDefaultAction() => _moveAction;

    // ITileOccupant 구현
    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => true;
    public bool IsCover => false;

#pragma warning disable 67
    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;
#pragma warning restore 67

    public event Action<Unit> OnUnitInitialized;

    private void Awake() => _collider = GetComponent<Collider>();

    // ========================================================================
    // 5. 초기화 (수정됨)
    // ========================================================================
    public void Initialize(UnitDataSO data, Faction faction)
    {
        Data = data;
        Faction = faction;
        gameObject.name = Data != null ? $"{Data.UnitName}" : "Unit";

        // HP 초기화
        if (Data != null)
        {
            CurrentHP = Data.MaxHP;

            // [Fix] SO의 ClassType 설정을 Unit 인스턴스에 적용
            // 이 부분이 빠져서 기본값인 Assault로 동작하고 있었습니다.
            ClassType = Data.Role;
        }
        else
        {
            CurrentHP = 10;
        }

        // 턴 상태 초기화
        CurrentMobility = Mobility;
        HasAttacked = false;
        HasStartedMoving = false;

        // 팀 컬러 설정
        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (faction == Faction.Player) renderer.material.color = Color.blue;
            else if (faction == Faction.Enemy) renderer.material.color = Color.red;
            else renderer.material.color = Color.green;
        }

        // 액션 초기화
        _actions.Clear();
        _moveAction = GetOrAddAction<MoveAction>();
        _attackAction = GetOrAddAction<AttackAction>();

        OnUnitInitialized?.Invoke(this);
    }

    private T GetOrAddAction<T>() where T : BaseAction
    {
        T action = GetComponent<T>();
        if (action == null)
        {
            action = gameObject.AddComponent<T>();
        }
        action.Initialize(this);

        if (!_actions.Contains(action))
        {
            _actions.Add(action);
        }
        return action;
    }

    // ... (이하 기존 로직 동일) ...
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
        CurrentMobility = Mobility;
        HasAttacked = false;
        HasStartedMoving = false;

        if (_controller != null) await _controller.OnTurnStart();
    }

    public void OnTurnEnd() => _controller?.OnTurnEnd();

    public async UniTask MovePathAsync(List<GridCoords> path, MapManager mapManager)
    {
        if (path == null || path.Count == 0) return;

        HasStartedMoving = true;

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

    public void MarkAsAttacked()
    {
        HasAttacked = true;
    }

    private async UniTaskVoid RotateTo(Quaternion targetRot)
    {
        float t = 0; Quaternion startRot = transform.rotation;
        while (t < 1f) { t += Time.deltaTime * _rotateSpeed; transform.rotation = Quaternion.Slerp(startRot, targetRot, t); await UniTask.Yield(); }
        transform.rotation = targetRot;
    }

    public async UniTask TakeDamage(int damage)
    {
        CurrentHP = Mathf.Max(0, CurrentHP - damage);
        await UniTask.Delay(500);
        if (CurrentHP <= 0) Destroy(gameObject);
    }

    private float GetTargetHeight(float surfaceY) => (_collider == null) ? surfaceY : surfaceY + _collider.bounds.extents.y;

    private void OnDestroy()
    {
        if (_actions != null)
        {
            foreach (var action in _actions)
            {
                if (action != null)
                {
                    action.OnExit();
                    Destroy(action);
                }
            }
            _actions.Clear();
        }

        if (!_isSpawned) return;
        if (ServiceLocator.TryGet(out MapManager mapManager) && ServiceLocator.TryGet(out UnitManager unitManager))
        {
            mapManager.UnregisterUnit(this);
            unitManager.UnregisterUnit(this);
        }
    }
}