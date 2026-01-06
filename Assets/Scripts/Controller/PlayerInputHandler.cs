using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    // 이벤트만 발송 (SRP 준수)
    public event Action<GridCoords> OnHoverChanged;
    public event Action<GridCoords> OnMoveRequested;

    // [Fix] 컴파일 오류 해결: 취소 요청 이벤트 추가
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
        _mainCamera = Camera.main;
        _isInitialized = true;
        Debug.Log("[DEBUG] PlayerInputHandler Initialized.");
    }

    public void SetActive(bool active)
    {
        // Debug.Log($"[InputHandler] SetActive: {active}"); // 로그 너무 많으면 주석 처리

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

        // [Fix] ESC 키 입력 감지 -> 취소 이벤트 발송
        // (Input System 사용 시)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("[InputHandler] Cancel Requested (ESC)");
            OnCancelRequested?.Invoke();
        }

        HandleHover();
    }

    private void HandleHover()
    {
        // [수정 핵심] LayerMask 없이 좌표를 가져오는 로직 사용
        if (TryGetGridFromMouse(out GridCoords currentCoords))
        {
            if (!currentCoords.Equals(_lastHoveredCoords))
            {
                _lastHoveredCoords = currentCoords;
                // Debug.Log($"[InputHandler] Hover Changed: {currentCoords}");
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
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mapManager == null) return false;

        // InputSystem 마우스 위치
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCamera.ScreenPointToRay(mousePos);

        // 1차 시도: 그냥 쏜다 (LayerMask 따위 신경 안 씀. 뭐든 걸리면 OK)
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            coords = GridUtils.WorldToGrid(hit.point);
            // 맞은 곳에 타일 데이터가 있는지 확인 (이게 진짜 검증)
            if (_mapManager.HasTile(coords)) return true;
        }

        // 2차 시도 (안전장치): Collider가 없거나 레이어가 꼬였을 때
        // 강제로 바닥 높이(Y=0 혹은 현재 레벨)에 평면을 깔아서 교차점을 계산
        float planeHeight = 0f;

        Plane virtualFloor = new Plane(Vector3.up, new Vector3(0, planeHeight, 0));
        if (virtualFloor.Raycast(ray, out float distance))
        {
            Vector3 point = ray.GetPoint(distance);
            coords = GridUtils.WorldToGrid(point);

            // 데이터상으로 타일이 존재하면 유효한 클릭으로 인정
            if (_mapManager.HasTile(coords)) return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        if (_inputManager != null)
            _inputManager.OnCommandInput -= HandleClick;

        // 이벤트 구독자 정리 (안전을 위해)
        OnCancelRequested = null;
        OnHoverChanged = null;
        OnMoveRequested = null;
    }
}