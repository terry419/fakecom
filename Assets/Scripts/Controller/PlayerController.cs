using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem; // New Input System

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

    // [성능 최적화] 경로 계산 스로틀링용 변수
    private float _lastPathCalcTime;
    private const float PATH_CALC_INTERVAL = 0.1f;

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

        Debug.Log($"[PlayerController] Turn Start: {PossessedUnit.name}");

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

        // [성능 최적화] 매 프레임 계산 방지 (0.1초 간격)
        if (Time.time - _lastPathCalcTime < PATH_CALC_INTERVAL) return;
        _lastPathCalcTime = Time.time;

        // New Input System에서 마우스 위치 가져오기
        Vector2 mousePos = Mouse.current.position.ReadValue();
        UpdateMouseHover(mousePos);
    }

    // [유연성 개선] 인자로 좌표를 받도록 수정
    private void UpdateMouseHover(Vector2 screenPos)
    {
        // GetGridFromScreen도 인자를 받도록 변경됨
        if (!GetGridFromScreen(screenPos, out GridCoords targetCoords))
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

        // [Issue #1] Unit에 위임한 로직 사용
        int availableMove = PossessedUnit.GetAvailableMoveDistance();

        _validPathStep = fullPath.Take(availableMove).ToList();
        _invalidPathStep = fullPath.Skip(availableMove).ToList();

        _pathVisualizer?.ShowHybridPath(_validPathStep, _invalidPathStep);
    }

    private void CalculateReachableArea()
    {
        if (_mapManager == null || PossessedUnit == null) return;

        // [Issue #1] Unit에 위임한 로직 사용 (중복 제거)
        int range = PossessedUnit.GetAvailableMoveDistance();

        Debug.Log($"[PlayerController] Calc Reachable. Range:{range}");

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

            if (PossessedUnit.CurrentAP <= 0)
            {
                Debug.Log("AP Depleted -> Turn End");
                EndTurn();
            }
            else
            {
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

    // [유연성 개선] 외부 인자(screenPos)를 받도록 롤백
    private bool GetGridFromScreen(Vector2 screenPos, out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null || _mapManager == null) return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPos);

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