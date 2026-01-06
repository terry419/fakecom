using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour, IInitializable
{
    public Unit PossessedUnit { get; private set; }
    [SerializeField] private PathVisualizer _pathVisualizer;

    private Camera _mainCamera;
    private MapManager _mapManager;
    private InputManager _inputManager;
    private CameraController _cameraController;

    private bool _isMyTurn = false;
    private bool _turnEnded = false;
    private UniTaskCompletionSource _turnCompletionSource;

    private HashSet<GridCoords> _cachedReachableTiles;
    private GridCoords _lastHoveredCoords;

    private List<GridCoords> _validPathStep;
    private List<GridCoords> _invalidPathStep;

    private void Awake() => _mainCamera = Camera.main;

    public UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
        _mapManager = ServiceLocator.Get<MapManager>();
        _inputManager = ServiceLocator.Get<InputManager>();
        if (_pathVisualizer == null) _pathVisualizer = ServiceLocator.Get<PathVisualizer>();
        return UniTask.CompletedTask;
    }

    public async UniTask<bool> Possess(Unit unit)
    {
        if (unit == null) return false;
        PossessedUnit = unit;
        _isMyTurn = true;

        if (_mapManager == null) ServiceLocator.TryGet(out _mapManager);
        if (_inputManager == null) ServiceLocator.TryGet(out _inputManager);
        if (_pathVisualizer == null) ServiceLocator.TryGet(out _pathVisualizer);
        if (_cameraController == null && !ServiceLocator.TryGet(out _cameraController))
            _cameraController = FindObjectOfType<CameraController>();

        if (_cameraController != null) _cameraController.SetTarget(unit.transform);

        CalculateReachableArea();
        ConnectInput();
        await UniTask.Yield();
        return true;
    }

    public async UniTask Unpossess()
    {
        DisconnectInput();
        PossessedUnit = null;
        _isMyTurn = false;
        CleanupVisuals(); // 시각 효과 정리
        await UniTask.Yield();
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;
        _isMyTurn = true;
        _turnEnded = false;
        _turnCompletionSource = new UniTaskCompletionSource();
        ConnectInput();
        CalculateReachableArea();
        await _turnCompletionSource.Task;
        DisconnectInput();
        CleanupVisuals();
    }

    public void OnTurnEnd() => _isMyTurn = false;

    private void Update()
    {
        if (!_isMyTurn || _turnEnded || PossessedUnit == null) return;
        UpdateMouseHover();
    }

    private void UpdateMouseHover()
    {
        // 1. 마우스 위치의 그리드 좌표 확인
        if (!GetGridFromScreen(out GridCoords targetCoords))
        {
            // 경로 데이터가 있다면 클리어
            if (_validPathStep != null || _invalidPathStep != null)
            {
                ClearPathOnly();
            }
            return;
        }

        if (targetCoords.Equals(_lastHoveredCoords)) return;
        _lastHoveredCoords = targetCoords;

        if (_mapManager == null) return;

        // 2. 경로 계산
        List<GridCoords> fullPath = Pathfinder.FindPath(PossessedUnit.Coordinate, targetCoords, _mapManager);

        if (fullPath == null || fullPath.Count == 0)
        {
            // 경로를 찾을 수 없는 경우 표시 제거
            ClearPathOnly();
            return;
        }

        int maxRange = PossessedUnit.HasStartedMoving
            ? PossessedUnit.CurrentMobility
            : ((PossessedUnit.CurrentAP >= 1) ? PossessedUnit.Mobility : 0);

        int validCount = 0;
        bool physicallyBlocked = false;

        for (int i = 0; i < fullPath.Count; i++)
        {
            GridCoords step = fullPath[i];

            // (A) 유닛 충돌 체크 (본인 제외)
            if (_mapManager.HasUnit(step) && step != PossessedUnit.Coordinate)
            {
                physicallyBlocked = true;
                break;
            }

            // (B) 장애물 체크
            Tile t = _mapManager.GetTile(step);
            if (t != null && !t.IsWalkable)
            {
                physicallyBlocked = true;
                break;
            }

            // (C) 이동 거리 체크
            if (i >= maxRange)
            {
                // 범위를 초과한 경우 validCount를 늘리지 않음 (붉은색 표시용)
            }
            else
            {
                validCount++;
            }
        }

        _validPathStep = fullPath.Take(validCount).ToList();

        if (physicallyBlocked)
        {
            // 물리적으로 막힌 경우 첫 번째 막힌 타일만 표시
            if (validCount < fullPath.Count)
                _invalidPathStep = new List<GridCoords> { fullPath[validCount] };
            else
                _invalidPathStep = new List<GridCoords>();
        }
        else
        {
            // AP 부족으로 못가는 나머지 경로 전체 표시
            _invalidPathStep = fullPath.Skip(validCount).ToList();
        }

        _pathVisualizer?.ShowHybridPath(_validPathStep, _invalidPathStep);
    }

    // 마우스가 그리드를 벗어나거나 이동이 시작될 때 경로만 지우는 함수
    private void ClearPathOnly()
    {
        _validPathStep = null;
        _invalidPathStep = null;
        _pathVisualizer?.ClearPath();
        _lastHoveredCoords = default;
    }

    // 모든 시각 효과 정리 (턴 종료, 이동 시작 시 호출)
    private void CleanupVisuals()
    {
        _cachedReachableTiles = null;
        ClearPathOnly();
        _pathVisualizer?.ClearAll(); // 도달 가능 영역까지 전부 삭제
    }

    private void CalculateReachableArea()
    {
        if (_mapManager == null) _mapManager = ServiceLocator.Get<MapManager>();
        if (_mapManager == null || PossessedUnit == null) return;

        int range = PossessedUnit.HasStartedMoving ? PossessedUnit.CurrentMobility :
                   ((PossessedUnit.CurrentAP >= 1) ? PossessedUnit.Mobility : 0);

        _cachedReachableTiles = Pathfinder.GetReachableTiles(PossessedUnit.Coordinate, range, _mapManager);

        if (_pathVisualizer != null)
            _pathVisualizer.ShowReachable(_cachedReachableTiles, excludeCoords: PossessedUnit.Coordinate);
    }

    private void ConnectInput()
    {
        if (_inputManager == null) _inputManager = ServiceLocator.Get<InputManager>();
        if (_inputManager != null)
        {
            _inputManager.OnCommandInput -= HandleCommand;
            _inputManager.OnCommandInput += HandleCommand;
        }
    }

    private void DisconnectInput()
    {
        if (_inputManager != null) _inputManager.OnCommandInput -= HandleCommand;
    }

    private void HandleCommand(Vector2 screenPos)
    {
        if (!_isMyTurn || _turnEnded) return;

        if (GetGridFromScreen(out GridCoords targetCoords))
        {
            if (targetCoords != PossessedUnit.Coordinate && _mapManager.HasUnit(targetCoords))
            {
                Debug.LogWarning("Invalid Target (Occupied by Unit)!");
                return;
            }
            Tile t = _mapManager.GetTile(targetCoords);
            if (t != null && !t.IsWalkable)
            {
                Debug.LogWarning("Invalid Target (Obstacle)!");
                return;
            }
        }

        if (_validPathStep != null && _validPathStep.Count > 0)
        {
            if (_invalidPathStep != null && _invalidPathStep.Count > 0)
            {
                Debug.LogWarning("Target is invalid (Too far or Blocked)!");
                return;
            }
            ExecuteMove(_validPathStep).Forget();
        }
    }

    private async UniTaskVoid ExecuteMove(List<GridCoords> path)
    {
        try
        {
            DisconnectInput();
            CleanupVisuals(); // 이동 중 시각 효과 제거
            await PossessedUnit.MovePathAsync(path, _mapManager);

            if (PossessedUnit.CurrentAP <= 0) EndTurn();
            else
            {
                CalculateReachableArea(); // 이동 후 도달 가능 영역 갱신
                ConnectInput();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerController] Move Error: {ex.Message}");
            EndTurn();
        }
    }

    private bool GetGridFromScreen(out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mapManager == null) return false;

        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            coords = GridUtils.WorldToGrid(hit.point);
            return _mapManager.HasTile(coords);
        }
        return false;
    }

    public void EndTurn()
    {
        if (_turnEnded) return;
        _turnEnded = true;
        _turnCompletionSource?.TrySetResult();
    }
}