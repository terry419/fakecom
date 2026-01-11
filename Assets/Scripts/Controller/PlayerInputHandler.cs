using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // 이벤트만 발송 (SRP 준수)
    public event Action<GridCoords> OnHoverChanged;
    public event Action<GridCoords> OnMoveRequested;
    public event Action OnCancelRequested;

    private InputManager _inputManager;
    private MapManager _mapManager;
    private Camera _mainCamera;

    private GridCoords _lastHoveredCoords;
    private bool _isActive = false;
    private bool _isInitialized = false;

    // 초기화 (ServiceLocator 등에서 호출)
    public void Initialize(InputManager inputMgr, MapManager mapMgr)
    {
        _inputManager = inputMgr;
        _mapManager = mapMgr;

        // [Optimization] Camera.main은 내부적으로 FindObjectWithTag를 사용하므로
        // Initialize 시점에 한 번만 캐싱하여 성능 부하를 줄입니다.
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            Debug.LogError("[PlayerInputHandler] MainCamera not found! Input raycasting will fail.");
        }

        _isInitialized = true;
        Debug.Log("[DEBUG] PlayerInputHandler Initialized.");
    }

    public void SetActive(bool active)
    {
        if (!_isInitialized) return;
        if (_isActive == active) return;

        _isActive = active;

        if (_isActive)
        {
            if (_inputManager != null)
            {
                _inputManager.OnCommandInput -= HandleClick; // 중복 방지
                _inputManager.OnCommandInput += HandleClick;
            }
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

        // [Input] ESC 취소 요청
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // Debug.Log("[InputHandler] Cancel Requested (ESC)");
            OnCancelRequested?.Invoke();
        }

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
            Debug.Log($"[InputHandler] Move Requested: {targetCoords}");
            OnMoveRequested?.Invoke(targetCoords);
        }
    }

    // ================================================================
    // [핵심 로직] LayerMask에 의존하지 않는 안전한 좌표 계산
    // ================================================================
    private bool TryGetGridFromMouse(out GridCoords coords)
    {
        coords = default;

        // [Safety] 초기화 안 된 상태 방지 (혹은 런타임에 카메라가 바뀐 경우)
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null || _mapManager == null) return false;

        // InputSystem 마우스 위치
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        // 1차 시도: Raycast (LayerMask 무관)
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            coords = GridUtils.WorldToGrid(hit.point);
            if (_mapManager.HasTile(coords)) return true;
        }

        // 2차 시도: 가상 평면 (Collider 누락 대비)
        // Y=0 (또는 맵의 기준 높이) 평면과의 교차점 계산
        Plane virtualFloor = new Plane(Vector3.up, Vector3.zero);
        if (virtualFloor.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            coords = GridUtils.WorldToGrid(point);
            if (_mapManager.HasTile(coords)) return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (_inputManager != null)
            _inputManager.OnCommandInput -= HandleClick;

        OnCancelRequested = null;
        OnHoverChanged = null;
        OnMoveRequested = null;
    }
}