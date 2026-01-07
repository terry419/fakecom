using UnityEngine;
using System.Collections.Generic;

public class UnitActionVisualizer : MonoBehaviour
{
    private PathVisualizer _pathVisualizer;
    private MoveAction _moveAction;
    private AttackAction _attackAction;

    // 초기화 여부 플래그 (중요)
    private bool _isInitialized = false;

    private void Awake()
    {
        // 여기서는 아무것도 찾지 않습니다.
        _pathVisualizer = ServiceLocator.Get<PathVisualizer>();
    }

    // [핵심] Unit.cs가 액션 생성을 끝내고 호출해줄 메서드
    public void Initialize(MoveAction moveAction, AttackAction attackAction)
    {
        _moveAction = moveAction;
        _attackAction = attackAction;
        _isInitialized = true;

        // 초기화 즉시 구독 시작
        SubscribeEvents();

        Debug.Log($"[Visualizer] Initialized via Injection. MoveAction Linked: {_moveAction != null}");
    }

    private void OnEnable()
    {
        // 초기화가 된 상태에서만 활성화 시 구독
        if (_isInitialized)
        {
            SubscribeEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        // 중복 구독 방지
        UnsubscribeEvents();

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

    // --- Event Handlers (동일) ---

    private void HandleShowReachable(HashSet<GridCoords> tiles, GridCoords center)
    {
        if (_pathVisualizer) _pathVisualizer.ShowReachable(tiles, center);
    }

    private void HandleShowPath(List<GridCoords> validPath, List<GridCoords> invalidPath)
    {
        if (validPath == null && invalidPath == null) _pathVisualizer?.ClearPath();
        else _pathVisualizer?.ShowHybridPath(validPath, invalidPath);
    }

    private void HandleClearVisuals() => _pathVisualizer?.ClearAll();

    private void HandleShowRange(Vector3 center, int range) => _pathVisualizer?.ShowRangeCircle(center, range);
    private void HandleHideRange() => _pathVisualizer?.HideRangeCircle();
}