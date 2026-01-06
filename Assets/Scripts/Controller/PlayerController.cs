using Cysharp.Threading.Tasks;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // ToList() 사용

public class PlayerController : MonoBehaviour, IInitializable
{
    public Unit PossessedUnit { get; private set; }

    // 의존성
    private MovementPlanner _planner;
    private PlayerInputHandler _inputHandler;
    private PathVisualizer _pathVisualizer;
    private CameraController _cameraController;

    // 상태
    private UniTaskCompletionSource _turnCompletionSource;
    private bool _isMyTurn = false;

    public UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);

        var mapManager = ServiceLocator.Get<MapManager>();
        var inputManager = ServiceLocator.Get<InputManager>();

        // 1. Planner & InputHandler 생성 및 초기화
        _planner = new MovementPlanner(mapManager);

        _inputHandler = gameObject.GetComponent<PlayerInputHandler>();
        if (_inputHandler == null) _inputHandler = gameObject.AddComponent<PlayerInputHandler>();
        _inputHandler.Initialize(inputManager, mapManager);

        _pathVisualizer = ServiceLocator.Get<PathVisualizer>();
        _cameraController = FindObjectOfType<CameraController>();

        // 2. 이벤트 연결
        _inputHandler.OnHoverChanged += OnHoverChanged;
        _inputHandler.OnMoveRequested += OnMoveRequested;

        return UniTask.CompletedTask;
    }

    // --- Flow Control ---

    public UniTask<bool> Possess(Unit unit)
    {
        if (unit == null) return UniTask.FromResult(false);
        PossessedUnit = unit;
        _cameraController?.SetTarget(unit.transform);
        // Note: 입력 활성화는 OnTurnStart에서 수행
        return UniTask.FromResult(true);
    }

    public async UniTask Unpossess()
    {
        CleanupTurn();
        PossessedUnit = null;
        await UniTask.Yield();
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;
        _isMyTurn = true;
        _turnCompletionSource = new UniTaskCompletionSource();

        RefreshReachability();
        _inputHandler.SetActive(true);

        await _turnCompletionSource.Task;
        CleanupTurn();
    }

    public void EndTurn()
    {
        if (!_isMyTurn) return;
        _turnCompletionSource?.TrySetResult();
    }

    private void CleanupTurn()
    {
        _isMyTurn = false;
        _inputHandler.SetActive(false);
        _pathVisualizer?.ClearAll();
        _planner.InvalidatePathCache();
    }

    private void RefreshReachability()
    {
        if (PossessedUnit == null) return;
        _planner.CalculateReachableArea(PossessedUnit);
        // PathVisualizer가 IEnumerable을 받으면 그대로, List를 받으면 ToList() 필요 (여기선 안전하게 그대로 전달 가정)
        _pathVisualizer?.ShowReachable(_planner.CachedReachableTiles, PossessedUnit.Coordinate);
    }

    // --- Event Handlers ---

    private void OnHoverChanged(GridCoords target)
    {
        if (!_isMyTurn || PossessedUnit == null) return;

        PathCalculationResult result = _planner.CalculatePath(PossessedUnit, target);

        if (result.HasAnyPath)
        {
            // [Fix] IReadOnlyList -> List 변환 (CS1503 해결)
            _pathVisualizer?.ShowHybridPath(result.ValidPath.ToList(), result.InvalidPath.ToList());
        }
        else
        {
            _pathVisualizer?.ClearPath();
        }
    }

    private void OnMoveRequested(GridCoords target)
    {
        if (!_isMyTurn || PossessedUnit == null) return;

        PathCalculationResult result = _planner.CalculatePath(PossessedUnit, target);

        if (!result.IsValidMovePath)
        {
            Debug.LogWarning("이동 불가: 경로가 유효하지 않음");
            return;
        }
        if (!result.CanUnitAfford(PossessedUnit.CurrentAP))
        {
            Debug.LogWarning("이동 불가: AP 부족");
            return;
        }

        ExecuteMove(result).Forget();
    }

    private async UniTaskVoid ExecuteMove(PathCalculationResult result)
    {
        try
        {
            _inputHandler.SetActive(false);
            _pathVisualizer?.ClearAll();

            // [Fix] List 변환하여 전달
            await PossessedUnit.MovePathAsync(result.ValidPath.ToList(), ServiceLocator.Get<MapManager>());

            if (PossessedUnit != null && PossessedUnit.CurrentAP > 0)
            {
                RefreshReachability();
                _inputHandler.SetActive(true);
            }
            else
            {
                EndTurn();
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[PlayerController] Move Error: {ex.Message}");
            EndTurn(); // 에러 시에도 턴이 멈추지 않게 종료 처리
        }
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnHoverChanged -= OnHoverChanged;
            _inputHandler.OnMoveRequested -= OnMoveRequested;
        }
        _turnCompletionSource?.TrySetResult();
    }
}