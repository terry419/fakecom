using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UnitMovementSystem))]
public class Unit : MonoBehaviour, ITileOccupant
{
    // ... (기존 필드와 동일) ...
    public Faction Faction { get; private set; }
    public GridCoords Coordinate { get; private set; }
    public GridCoords Coords => Coordinate;
    public UnitDataSO Data { get; private set; }

    [Header("Class Settings")]
    public ClassType ClassType = ClassType.Assault;

    private UnitStatus _status;
    public UnitStatus Status => _status ? _status : (_status = GetComponent<UnitStatus>());

    private UnitMovementSystem _movementSystem;
    public UnitMovementSystem MovementSystem => _movementSystem ? _movementSystem : (_movementSystem = GetComponent<UnitMovementSystem>());

    // Bridge Properties
    public int CurrentHP => Status.CurrentHP;
    public int CurrentMobility => Status.RemainingMobility;
    public bool HasAttacked => Status.HasAttacked;
    public bool HasStartedMoving => Status.HasMoved;
    public int Mobility => Data != null ? Data.Mobility : 5;

    private IUnitController _controller;
    public IUnitController Controller => _controller;

    private List<BaseAction> _actions = new List<BaseAction>();
    private MoveAction _moveAction;
    private AttackAction _attackAction;

    private UnitActionVisualizer _actionVisualizer;

    public IReadOnlyList<BaseAction> GetActions() => _actions.AsReadOnly();
    public MoveAction GetMoveAction() => _moveAction;
    public AttackAction GetAttackAction() => _attackAction;
    public BaseAction GetDefaultAction() => _moveAction;

    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => true;
    public bool IsCover => false;

#pragma warning disable 67
    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;
#pragma warning restore 67

    public event Action<Unit> OnUnitInitialized;
    private UnitDamageFeedback _damageFeedback;

    private void Awake()
    {
        _status = GetComponent<UnitStatus>();
        _movementSystem = GetComponent<UnitMovementSystem>();
    }

    public void Initialize(UnitDataSO data, Faction faction)
    {
        Data = data;
        Faction = faction;
        gameObject.name = Data != null ? $"{Data.UnitName}" : "Unit";

        if (Data != null) ClassType = Data.Role;

        var renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            if (faction == Faction.Player) renderer.material.color = Color.blue;
            else if (faction == Faction.Enemy) renderer.material.color = Color.red;
            else renderer.material.color = Color.green;
        }

        if (MovementSystem != null) MovementSystem.Initialize(this);

        // 1. Action 생성
        _actions.Clear();
        _moveAction = GetOrAddAction<MoveAction>();
        _attackAction = GetOrAddAction<AttackAction>();

        // 2. Visualizer 연결
        _actionVisualizer = GetComponent<UnitActionVisualizer>();
        if (_actionVisualizer == null)
        {
            _actionVisualizer = gameObject.AddComponent<UnitActionVisualizer>();
        }
        _actionVisualizer.Initialize(_moveAction, _attackAction);

        _damageFeedback = GetComponent<UnitDamageFeedback>();
        if (_damageFeedback == null)
        {
            _damageFeedback = gameObject.AddComponent<UnitDamageFeedback>();
        }
        _damageFeedback.Initialize(Status.HealthSystem);

        OnUnitInitialized?.Invoke(this);

        // ---------------------------------------------------------
        // [Step 4 Fix] 로컬 컨트롤러(AI) 자동 감지 및 연결 로직
        // Player는 UnitManager가 넣어주지만, Enemy는 스스로 찾아야 함
        // ---------------------------------------------------------
        if (_controller == null)
        {
            var localController = GetComponent<IUnitController>();
            if (localController != null)
            {
                // AI 컨트롤러는 비동기 대기 없이 즉시 소유권 등록
                localController.Possess(this).Forget();
            }
        }
    }

    // ... (이하 기존 메서드 동일) ...
    private T GetOrAddAction<T>() where T : BaseAction
    {
        T action = GetComponent<T>();
        if (action == null) action = gameObject.AddComponent<T>();
        action.Initialize(this);
        if (!_actions.Contains(action)) _actions.Add(action);
        return action;
    }

    public void SpawnOnMap(GridCoords coords)
    {
        if (!ServiceLocator.TryGet(out MapManager mapManager))
            throw new InvalidOperationException($"[{nameof(Unit)}] MapManager not found.");

        Coordinate = coords;

        Vector3 worldPos = GridUtils.GridToWorld(coords);
        float y = worldPos.y;
        if (TryGetComponent<Collider>(out var col)) y += col.bounds.extents.y;
        transform.position = new Vector3(worldPos.x, y, worldPos.z);

        if (Data != null) gameObject.name = $"{Data.UnitName}_{Coordinate} ({Faction})";
    }

    public void OnAddedToTile(Tile tile) => Coordinate = tile.Coordinate;
    public void OnRemovedFromTile(Tile tile) { }

    public void SetController(IUnitController controller)
    {
        if (_controller != null && _controller != controller) _controller.Unpossess();
        _controller = controller;
        // Possess는 컨트롤러 측에서 호출하는 것이 안전함 (순환 호출 방지)
    }

    public async UniTask OnTurnStart()
    {
        if (_controller != null) await _controller.OnTurnStart();
    }

    public void OnTurnEnd() => _controller?.OnTurnEnd();

    public async UniTask MovePathAsync(List<GridCoords> path, MapManager mapManager)
    {
        if (path == null || path.Count == 0) return;

        if (Status != null)
        {
            Status.HasMoved = true;
            Status.ConsumeMobility(path.Count);
        }

        if (MovementSystem != null)
        {
            await MovementSystem.MoveAlongPathAsync(path, mapManager);
            if (path.Count > 0)
            {
                Coordinate = path[path.Count - 1];
            }
        }
    }

    public void MarkAsAttacked()
    {
        if (Status != null) Status.HasAttacked = true;
    }

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

        if (ServiceLocator.TryGet(out MapManager mapManager) && ServiceLocator.TryGet(out UnitManager unitManager))
        {
            mapManager.UnregisterUnit(this);
            unitManager.UnregisterUnit(this);
        }
    }
}