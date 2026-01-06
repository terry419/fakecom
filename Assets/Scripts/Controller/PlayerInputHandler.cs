using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    public event Action<GridCoords> OnHoverChanged;
    public event Action<GridCoords> OnMoveRequested;

    private InputManager _inputManager;
    private MapManager _mapManager;
    private Camera _mainCamera;

    private GridCoords _lastHoveredCoords;
    private bool _isActive = false;
    private bool _isInitialized = false;

    // 1000f 상수화
    private const float RAYCAST_DISTANCE = 1000f;

    public void Initialize(InputManager inputMgr, MapManager mapMgr)
    {
        _inputManager = inputMgr;
        _mapManager = mapMgr;
        _mainCamera = Camera.main;
        _isInitialized = true;
    }

    public void SetActive(bool active)
    {
        if (!_isInitialized) return;
        if (_isActive == active) return;

        _isActive = active;

        if (_isActive)
        {
            if (_inputManager != null) _inputManager.OnCommandInput += HandleClick;
        }
        else
        {
            if (_inputManager != null) _inputManager.OnCommandInput -= HandleClick;
            _lastHoveredCoords = default;
        }
    }

    private void Update()
    {
        if (!_isActive || !_isInitialized) return;
        HandleHover();
    }

    private void HandleHover()
    {
        if (TryGetGridFromMouse(out GridCoords currentCoords))
        {
            if (!currentCoords.Equals(_lastHoveredCoords))
            {
                _lastHoveredCoords = currentCoords;
                OnHoverChanged?.Invoke(currentCoords);
            }
        }
    }

    private void HandleClick(Vector2 screenPos)
    {
        if (!_isActive || !_isInitialized) return;

        if (TryGetGridFromMouse(out GridCoords targetCoords))
        {
            OnMoveRequested?.Invoke(targetCoords);
        }
    }

    // [수정됨] 원본 로직 복원: LayerMask 없이 Raycast 후 MapManager에 타일 존재 여부 확인
    private bool TryGetGridFromMouse(out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mapManager == null) return false;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        // 레이어 구분 없이 Ray를 쏘고, 맞은 위치가 유효한 타일인지 확인하는 원본 방식
        if (Physics.Raycast(ray, out RaycastHit hit, RAYCAST_DISTANCE))
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