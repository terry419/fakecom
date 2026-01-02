// 경로: Assets/Scripts/Controllers/PlayerController.cs
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

    private bool _isMyTurn = false;
    private bool _turnEnded = false;
    private UniTaskCompletionSource _turnCompletionSource;

    private HashSet<GridCoords> _cachedReachableTiles;
    private GridCoords _lastHoveredCoords;

    // 경로를 두 개로 분리하여 관리
    private List<GridCoords> _validPathStep;    // 갈 수 있는 길
    private List<GridCoords> _invalidPathStep;  // AP 부족으로 못 가는 길

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

        Debug.Log("[PlayerController] Turn Finished Task Awaited.");
        DisconnectInput();
        CleanupVisuals();
    }

    public void OnTurnEnd()
    {
        Debug.Log("[PlayerController] OnTurnEnd Called.");
        _isMyTurn = false;
    }

    private void Update()
    {
        if (!_isMyTurn || _turnEnded || PossessedUnit == null) return;
        UpdateMouseHover();
    }

    // [Hybrid Path Logic] AP 기준으로 경로를 파랑/빨강으로 나눔
    private void UpdateMouseHover()
    {
        if (!GetGridFromScreen(Input.mousePosition, out GridCoords targetCoords))
        {
            // 타일 밖으로 나감
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

        // 1. 전체 경로 계산
        List<GridCoords> fullPath = Pathfinder.FindPath(PossessedUnit.Coordinate, targetCoords, _mapManager);

        if (fullPath == null || fullPath.Count == 0)
        {
            _pathVisualizer?.ClearPath();
            return;
        }

        // 2. AP 기준으로 경로 분할 (Hybrid Logic)
        int currentAP = PossessedUnit.CurrentAP;

        // Take: 앞에서부터 AP만큼 (이동 가능)
        _validPathStep = fullPath.Take(currentAP).ToList();

        // Skip: AP 이후 나머지 (이동 불가)
        _invalidPathStep = fullPath.Skip(currentAP).ToList();

        // 3. 시각화 요청
        _pathVisualizer?.ShowHybridPath(_validPathStep, _invalidPathStep);
    }

    private void CalculateReachableArea()
    {
        if (_mapManager == null || PossessedUnit == null) return;

        Debug.Log("[PlayerController] Calculating Reachable Area...");
        _cachedReachableTiles = Pathfinder.GetReachableTiles(PossessedUnit.Coordinate, PossessedUnit.CurrentAP, _mapManager);

        Debug.Log($"[PlayerController] Reachable Tiles Count: {_cachedReachableTiles?.Count ?? 0}");
        _pathVisualizer?.ShowReachable(_cachedReachableTiles, excludeCoords: PossessedUnit.Coordinate);
    }

    private void ConnectInput()
    {
        if (_inputManager == null) return;
        _inputManager.OnCommandInput -= HandleCommand; // 중복 방지
        _inputManager.OnCommandInput += HandleCommand;
        Debug.Log("[PlayerController] Input Connected.");
    }

    private void DisconnectInput()
    {
        if (_inputManager == null) return;
        _inputManager.OnCommandInput -= HandleCommand;
        Debug.Log("[PlayerController] Input Disconnected.");
    }

    private void HandleCommand(Vector2 screenPos)
    {
        if (!_isMyTurn || _turnEnded)
        {
            Debug.LogWarning("[PlayerController] Click ignored: Not my turn or turn ended.");
            return;
        }

        // 1. 클릭한 곳이 유효한 경로(파란색) 끝점인지 확인
        //    (간단한 처리를 위해, 유효 경로가 존재하고 빨간 경로가 없을 때만 이동 허용)
        if (_validPathStep != null && _validPathStep.Count > 0)
        {
            // 클릭한 지점이 '이동 불가' 영역에 포함되어 있다면?
            // (즉, 마우스가 빨간 경로 위에 있다면 이동 불가 처리)
            if (_invalidPathStep != null && _invalidPathStep.Count > 0)
            {
                Debug.Log("[PlayerController] Cannot move: Target is too far (Not enough AP).");
                return;
            }

            Debug.Log($"[PlayerController] Executing Move. Steps: {_validPathStep.Count}");
            ExecuteMove(_validPathStep).Forget();
        }
        else
        {
            Debug.Log("[PlayerController] No valid path to move.");
        }
    }

    private async UniTaskVoid ExecuteMove(List<GridCoords> path)
    {
        try
        {
            DisconnectInput();
            CleanupVisuals();

            await PossessedUnit.MovePathAsync(path, _mapManager);

            Debug.Log($"[PlayerController] Move Complete. Remaining AP: {PossessedUnit.CurrentAP}");

            if (PossessedUnit.CurrentAP <= 0)
            {
                Debug.Log("[PlayerController] AP Depleted. Ending Turn.");
                EndTurn();
            }
            else
            {
                // 아직 AP 남음 -> 다시 입력 대기
                CalculateReachableArea();
                ConnectInput();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PlayerController] Move Error: {ex.Message}\n{ex.StackTrace}");
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

    private bool GetGridFromScreen(Vector2 screenPos, out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null || _mapManager == null) return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPos);
        // 레이어 마스크 제거 (모든 충돌체 검사)
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            // Debug: 무엇에 맞았는지 확인하고 싶다면 주석 해제
            // Debug.DrawLine(_mainCamera.transform.position, hit.point, Color.yellow, 0.1f);

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