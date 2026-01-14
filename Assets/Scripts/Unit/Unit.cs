using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(UnitMovementSystem))]
public class Unit : MonoBehaviour, ITileOccupant
{
    // ... (���� �ʵ� ����) ...
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

    private UnitActionVisualizer _visualizer;
    private UnitDamageFeedback _damageFeedback;

    public event Action<Unit> OnUnitInitialized;
    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;

    // ��� ������Ƽ (���� ����)
    private AmmoDataSO _overrideAmmo;
    public AmmoDataSO CurrentAmmo
    {
        get
        {
            if (_overrideAmmo != null) return _overrideAmmo;
            if (Data != null && Data.MainWeapon != null) return Data.MainWeapon.DefaultAmmo;
            return null;
        }
        set => _overrideAmmo = value;
    }

    private ArmorDataSO _overrideArmor;
    public ArmorDataSO CurrentArmor
    {
        get
        {
            if (_overrideArmor != null) return _overrideArmor;
            if (Data != null) return Data.BodyArmor;
            return null;
        }
        set => _overrideArmor = value;
    }

    public void EquipAmmo(AmmoDataSO newAmmo) => _overrideAmmo = newAmmo;
    public void ResetToDefaultAmmo() => _overrideAmmo = null;
    public void EquipArmor(ArmorDataSO newArmor) => _overrideArmor = newArmor;
    public void ResetToDefaultArmor() => _overrideArmor = null;

    private void Awake()
    {
        _status = GetComponent<UnitStatus>();
        _movementSystem = GetComponent<UnitMovementSystem>();
        _visualizer = GetComponentInChildren<UnitActionVisualizer>();
        _damageFeedback = GetComponent<UnitDamageFeedback>();
    }

    // �ʱ�ȭ �޼���
    public void Initialize(GridCoords coords, UnitDataSO data)
    {
        Initialize(coords, data, Faction.Enemy);
    }

    public void Initialize(GridCoords coords, UnitDataSO data, Faction faction)
    {
        Coordinate = coords;
        Data = data;
        Faction = faction;

        gameObject.name = $"{Data.UnitName}_{Coordinate} ({Faction})";

        SpawnOnMap();
        OnUnitInitialized?.Invoke(this);
    }

    public void SpawnOnMap()
    {
        if (ServiceLocator.TryGet<MapManager>(out var mapManager))
        {
            mapManager.RegisterUnit(Coordinate, this);
        }

        transform.position = GridUtils.GridToWorld(Coordinate);
        AlignToGround();

        if (MovementSystem != null)
        {
            // [Fix] Error CS1503: cannot convert 'GridCoords' to 'Unit'
            // MovementSystem.Initialize�� Unit�� �Ű������� �䱸�ϴ� ������ ���Դϴ�.
            MovementSystem.Initialize(this);
        }

        _moveAction = GetOrAddAction<MoveAction>();
        _attackAction = GetOrAddAction<AttackAction>();

        if (_visualizer != null)
        {
            _visualizer.Initialize(_moveAction, _attackAction);
        }

        if (_damageFeedback != null && Status != null)
        {
            _damageFeedback.Initialize(Status.HealthSystem);
        }

        // 6. [복구] AI 컨트롤러 자동 감지 및 연결
        var autoController = GetComponent<IUnitController>();
        if (autoController != null && _controller == null)
        {
            // Possess()를 호출하면, BaseUnitController 내부에서
            // unit.SetController(this)를 호출하여 양방향 연결이 완료됩니다.
            autoController.Possess(this).Forget();
        }
    }
    private void AlignToGround()
    {
        // 1. ������ Ȥ�� �ݶ��̴��� �ٿ�带 ������
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        // ��ü �ٿ�� ���
        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        // 2. ���� �߹ٴ� ���� (World Y)
        float feetY = combinedBounds.min.y;

        // 3. ��ǥ ���� (Pivot ��ġ)
        // GridToWorld�� ��ȯ�ϴ� ���� Ÿ���� ��� ǥ���̶�� ����
        float targetY = transform.position.y;

        // 4. ���̸�ŭ ����
        float diff = targetY - feetY;
        transform.position += new Vector3(0, diff, 0);
    }

    public T GetOrAddAction<T>() where T : BaseAction
    {
        T action = GetComponent<T>();
        if (action == null)
        {
            action = gameObject.AddComponent<T>();
        }

        if (!_actions.Contains(action))
        {
            _actions.Add(action);
            action.Initialize(this);
        }
        return action;
    }

    public T GetDefaultAction<T>() where T : BaseAction
    {
        return GetOrAddAction<T>();
    }

    // [Fix] Error CS0411 in PlayerController: ���׸� �߷� �Ұ� �ذ�
    // MoveAction�� �⺻������ ��ȯ�ϴ� �����ε� �߰�
    public BaseAction GetDefaultAction()
    {
        return GetOrAddAction<MoveAction>();
    }

    public AttackAction GetAttackAction()
    {
        return _attackAction != null ? _attackAction : GetOrAddAction<AttackAction>();
    }

    public void SetController(IUnitController newController)
    {
        if (_controller != null && _controller != newController)
        {
            _controller.Unpossess();
        }
        _controller = newController;
    }

    public void AssignController(IUnitController newController) => SetController(newController);

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
        if (ServiceLocator.TryGet<MapManager>(out var mapManager))
        {
            mapManager.UnregisterUnit(this);
        }

        if (ServiceLocator.TryGet<UnitManager>(out var unitManager))
        {
            unitManager.UnregisterUnit(this);
        }

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
    }

    public OccupantType Type => OccupantType.Unit;
    public bool IsBlockingMovement => !IsDead;
    public bool IsCover => true;

    public void OnAddedToTile(Tile tile) { }
    public void OnRemovedFromTile(Tile tile) { }

    private bool IsDead => Status != null && Status.IsDead;
}