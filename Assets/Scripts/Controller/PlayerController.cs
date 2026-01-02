using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour, IUnitController
{
    public Unit PossessedUnit { get; private set; }

    private Camera _mainCamera;
    private MapManager _mapManager;
    private InputManager _inputManager;
    private PathVisualizer _pathVisualizer;
    private CameraController _cameraController;

    private bool _isMyTurn = false;
    private bool _turnEnded = false;
    private UniTaskCompletionSource _turnCompletionSource;

    private HashSet<GridCoords> _cachedReachableTiles;
    private GridCoords _lastHoveredCoords;

    private List<GridCoords> _validPathStep;
    private List<GridCoords> _invalidPathStep;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    public void Possess(Unit unit)
    {
        PossessedUnit = unit;
        ServiceLocator.TryGet(out _mapManager);
        ServiceLocator.TryGet(out _inputManager);
        ServiceLocator.TryGet(out _pathVisualizer);

        if (ServiceLocator.TryGet(out _cameraController))
        {
            _cameraController.SetTarget(unit.transform);
        }
        else
        {
            _cameraController = FindObjectOfType<CameraController>();
            if (_cameraController != null) _cameraController.SetTarget(unit.transform);
        }

        Debug.Log($"[PlayerController] Possessed Unit: {unit.name}");
    }

    public void Unpossess()
    {
        Debug.Log($"[PlayerController] Unpossess Unit: {PossessedUnit?.name}");
        DisconnectInput();
        PossessedUnit = null;
        _isMyTurn = false;
        CleanupVisuals();
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;

        Debug.Log($"[PlayerController] Turn Start: {PossessedUnit.name} (AP: {PossessedUnit.CurrentAP})");

        _isMyTurn = true;
        _turnEnded = false;
        _turnCompletionSource = new UniTaskCompletionSource();

        ConnectInput();
        CalculateReachableArea();

        await _turnCompletionSource.Task;

        Debug.Log("[PlayerController] Turn Finished.");
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
        if (!GetGridFromScreen(out GridCoords targetCoords))
        {
            if (_validPathStep != null || _invalidPathStep != null)
            {
                _validPathStep = null;
                _invalidPathStep = null;
                _pathVisualizer?.ClearPath();
                _lastHoveredCoords = default;
            }
            return;
        }

        if (targetCoords.Equals(_lastHoveredCoords)) return;
        _lastHoveredCoords = targetCoords;

        List<GridCoords> fullPath = Pathfinder.FindPath(PossessedUnit.Coordinate, targetCoords, _mapManager);

        if (fullPath == null || fullPath.Count == 0)
        {
            _pathVisualizer?.ClearPath();
            return;
        }

        // [Logic] 이동 가능 거리 계산 (선불제 로직 반영)
        int availableMove = 0;

        if (PossessedUnit.HasStartedMoving)
        {
            // 이미 이동 중이면 남은 이동력만큼만 더 갈 수 있음
            availableMove = PossessedUnit.CurrentMobility;
        }
        else
        {
            // 아직 이동 안 했으면, AP가 1 이상 있어야 전체 Mobility만큼 이동 가능
            availableMove = (PossessedUnit.CurrentAP >= 1) ? PossessedUnit.Mobility : 0;
        }

        _validPathStep = fullPath.Take(availableMove).ToList();
        _invalidPathStep = fullPath.Skip(availableMove).ToList();

        _pathVisualizer?.ShowHybridPath(_validPathStep, _invalidPathStep);
    }

    private void CalculateReachableArea()
    {
        if (_mapManager == null || PossessedUnit == null) return;

        // [Logic] 이동 가능 범위 계산 (선불제 로직 반영)
        // 위 UpdateMouseHover와 동일한 로직
        int range = 0;

        if (PossessedUnit.HasStartedMoving)
        {
            range = PossessedUnit.CurrentMobility;
        }
        else
        {
            range = (PossessedUnit.CurrentAP >= 1) ? PossessedUnit.Mobility : 0;
        }

        Debug.Log($"[PlayerController] Calc Reachable. AP:{PossessedUnit.CurrentAP}, Moved:{PossessedUnit.HasStartedMoving}, Range:{range}");

        _cachedReachableTiles = Pathfinder.GetReachableTiles(PossessedUnit.Coordinate, range, _mapManager);
        _pathVisualizer?.ShowReachable(_cachedReachableTiles, excludeCoords: PossessedUnit.Coordinate);
    }

    private void ConnectInput()
    {
        if (_inputManager == null) return;
        _inputManager.OnCommandInput -= HandleCommand;
        _inputManager.OnCommandInput += HandleCommand;
    }

    private void DisconnectInput()
    {
        if (_inputManager == null) return;
        _inputManager.OnCommandInput -= HandleCommand;
    }

    private void HandleCommand(Vector2 screenPos)
    {
        if (!_isMyTurn || _turnEnded) return;

        if (_validPathStep != null && _validPathStep.Count > 0)
        {
            // 빨간색 경로(이동 불가)가 포함되어 있으면 이동 금지
            if (_invalidPathStep != null && _invalidPathStep.Count > 0)
            {
                Debug.LogWarning("Target is too far!");
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
            CleanupVisuals();

            await PossessedUnit.MovePathAsync(path, _mapManager);

            // 이동 후 턴 종료 조건 체크
            // 1. AP가 아예 0이 되면 종료
            // 2. 이동력까지 다 써버리면? (보통은 턴 종료 안 하고 공격 기회 줌. 여기선 AP 기준)
            if (PossessedUnit.CurrentAP <= 0)
            {
                Debug.Log("AP Depleted -> Turn End");
                EndTurn();
            }
            else
            {
                // AP 남았으면 (또는 이동력 남았으면) 다시 입력 대기
                CalculateReachableArea();
                ConnectInput();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerController] Move Error: {ex.Message}");
            EndTurn();
        }
    }

    private void CleanupVisuals()
    {
        _cachedReachableTiles = null;
        _validPathStep = null;
        _invalidPathStep = null;
        _pathVisualizer?.ClearAll();
    }

    private bool GetGridFromScreen(out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null || _mapManager == null) return false;

        // InputManager를 통해야 하지만 Raycast용으로 직접 읽음
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