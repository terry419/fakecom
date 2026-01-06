using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // High-Level Events
    public event Action<GridCoords> OnHoverChanged;
    public event Action<GridCoords> OnMoveRequested;

    private InputManager _inputManager;
    private MapManager _mapManager;
    private Camera _mainCamera;

    private GridCoords _lastHoveredCoords;
    private bool _isActive = false;

    public void Initialize(InputManager inputMgr, MapManager mapMgr)
    {
        _inputManager = inputMgr;
        _mapManager = mapMgr;
        _mainCamera = Camera.main;
    }

    public void SetActive(bool active)
    {
        if (_isActive == active) return;
        _isActive = active;

        if (_isActive)
        {
            // InputManager.OnCommandInput 이벤트 구독
            _inputManager.OnCommandInput += HandleClick;
        }
        else
        {
            _inputManager.OnCommandInput -= HandleClick;
        }
    }

    // 유일한 Update 루프 사용자
    private void Update()
    {
        if (!_isActive) return;
        HandleHover();
    }

    private void HandleHover()
    {
        if (TryGetGridFromMouse(out GridCoords currentCoords))
        {
            // 좌표가 변경되었을 때만 이벤트 발송 (최적화)
            if (!currentCoords.Equals(_lastHoveredCoords))
            {
                _lastHoveredCoords = currentCoords;
                OnHoverChanged?.Invoke(currentCoords);
            }
        }
    }

    private void HandleClick(Vector2 screenPos)
    {
        if (!_isActive) return;

        // 클릭 시점의 좌표가 유효하다면 이동 요청 발송
        if (TryGetGridFromMouse(out GridCoords targetCoords))
        {
            OnMoveRequested?.Invoke(targetCoords);
        }
    }

    // Raycast 로직 캡슐화
    private bool TryGetGridFromMouse(out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mapManager == null) return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        // Raycast 로직 이동
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            coords = GridUtils.WorldToGrid(hit.point); 
            return _mapManager.HasTile(coords); 
        }
        return false;
    }

    private void OnDestroy()
    {
        if (_inputManager != null)
            _inputManager.OnCommandInput -= HandleClick;
    }
}