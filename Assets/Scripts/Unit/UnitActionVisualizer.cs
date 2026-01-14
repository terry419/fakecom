using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Unit(Logic)의 이벤트를 PathVisualizer(View) 명령으로 변환하는 어댑터
/// </summary>
public class UnitActionVisualizer : MonoBehaviour
{
    private PathVisualizer _pathVisualizer;
    private MoveAction _moveAction;
    private AttackAction _attackAction;

    private bool _isInitialized = false;

    // [Fix] Awake 제거. ServiceLocator가 준비되지 않았을 수 있음.

    public void Initialize(MoveAction moveAction, AttackAction attackAction)
    {
        // 1. 필요한 서비스(PathVisualizer)를 안전하게 확보
        // 만약 여기서도 없다면 ServiceLocator 문제임.
        if (ServiceLocator.TryGet(out _pathVisualizer))
        {
            Debug.Log($"[Visualizer] Connected to PathVisualizer.");
        }
        else
        {
            Debug.LogWarning($"[Visualizer] PathVisualizer not found in Scene Scope!");
        }

        _moveAction = moveAction;
        _attackAction = attackAction;
        _isInitialized = true;

        SubscribeEvents();
    }

    private void OnEnable()
    {
        if (_isInitialized) SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        UnsubscribeEvents(); // 중복 방지

        if (_moveAction != null)
        {
            _moveAction.OnShowReachable += HandleShowReachable;
            _moveAction.OnShowPath += HandleShowPath;
            _moveAction.OnClearVisuals += HandleClearVisuals;
        }

        if (_attackAction != null)
        {
            _attackAction.OnShowRange += HandleShowRange;
            _attackAction.OnHideRange += HandleHideRange;
        }
    }

    private void UnsubscribeEvents()
    {
        if (_moveAction != null)
        {
            _moveAction.OnShowReachable -= HandleShowReachable;
            _moveAction.OnShowPath -= HandleShowPath;
            _moveAction.OnClearVisuals -= HandleClearVisuals;
        }

        if (_attackAction != null)
        {
            _attackAction.OnShowRange -= HandleShowRange;
            _attackAction.OnHideRange -= HandleHideRange;
        }
    }

    // --- Event Adapters ---

    private void HandleShowReachable(HashSet<GridCoords> tiles, GridCoords center)
    {
        _pathVisualizer?.ShowReachable(tiles, center);
    }

    private void HandleShowPath(List<GridCoords> validPath, List<GridCoords> invalidPath)
    {
        if (validPath == null && invalidPath == null)
            _pathVisualizer?.ClearPath();
        else
            _pathVisualizer?.ShowHybridPath(validPath, invalidPath);
    }

    private void HandleClearVisuals() => _pathVisualizer?.ClearAll();

    private void HandleShowRange(Vector3 center, int range)
    {
        // [변환] Grid 거리(int) -> World 거리(float)
        float worldRadius = range * GridUtils.CELL_SIZE;
        _pathVisualizer?.ShowRangeCircle(center, worldRadius);
    }

    private void HandleHideRange() => _pathVisualizer?.HideRangeCircle();
}