using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class PlayerController : MonoBehaviour, IUnitController
{
    // ========================================================================
    // 1. 상태 및 캐싱
    // ========================================================================
    public Unit PossessedUnit { get; private set; }

    private bool _isMyTurn = false;
    private bool _turnEnded = false;
    private UniTaskCompletionSource _turnCompletionSource;

    // [필수 의존성]
    private Camera _mainCamera;
    private MapManager _mapManager;
    private InputManager _inputManager; // [Fix] 에러 원인: 이 변수가 없었음

    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _turnTimeoutSeconds = 60f;

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_groundLayerMask == 0) _groundLayerMask = LayerMask.GetMask("Default", "Ground", "Tile");
    }

    // ========================================================================
    // 2. IUnitController 구현
    // ========================================================================
    public void Possess(Unit unit)
    {
        PossessedUnit = unit;

        // 의존성 획득 (ServiceLocator)
        if (ServiceLocator.TryGet(out MapManager map)) _mapManager = map;
        if (ServiceLocator.TryGet(out InputManager input)) _inputManager = input;

        Debug.Log($"[{nameof(PlayerController)}] Possessed Unit: {unit.name}");
    }

    public void Unpossess()
    {
        DisconnectInput(); // 연결 해제
        PossessedUnit = null;
        _isMyTurn = false;
        _mapManager = null;
        _inputManager = null;
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;

        Debug.Log($"<color=cyan>[{nameof(PlayerController)}] Turn Start!</color>");

        _isMyTurn = true;
        _turnEnded = false;
        _turnCompletionSource = new UniTaskCompletionSource();

        // 턴 시작 시 입력 이벤트 구독
        ConnectInput();

        // 타임아웃 및 턴 대기
        var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(_turnTimeoutSeconds));
        var turnTask = _turnCompletionSource.Task;

        var result = await UniTask.WhenAny(turnTask, timeoutTask);

        // 턴 종료 시 입력 차단
        DisconnectInput();

        if (result == 1) // Timeout
        {
            Debug.LogWarning($"[{nameof(PlayerController)}] Turn Timed Out!");
            EndTurn();
        }
    }

    public void OnTurnEnd()
    {
        _isMyTurn = false;
        DisconnectInput();
        Debug.Log($"<color=gray>[{nameof(PlayerController)}] Turn End.</color>");
    }

    // ========================================================================
    // 3. 입력 이벤트 핸들링 (Event Driven)
    // ========================================================================

    private void ConnectInput()
    {
        if (_inputManager == null) return;

        // 중복 구독 방지를 위해 먼저 해제
        DisconnectInput();

        // [Fix] 변경된 이벤트 이름(OnSelectInput 등) 사용
        _inputManager.OnSelectInput += HandleSelect;
        _inputManager.OnCommandInput += HandleCommand;
        _inputManager.OnCancelInput += HandleCancel;
    }

    private void DisconnectInput()
    {
        if (_inputManager == null) return;

        _inputManager.OnSelectInput -= HandleSelect;
        _inputManager.OnCommandInput -= HandleCommand;
        _inputManager.OnCancelInput -= HandleCancel;
    }

    // [Fix] 에러 원인: 아래 핸들러 메서드들이 없었음

    // 좌클릭 핸들러
    private void HandleSelect(Vector2 screenPos)
    {
        if (!_isMyTurn) return;
        // 추후 구현: 유닛 정보 표시 등
    }

    // 우클릭 핸들러 (이동 명령)
    private void HandleCommand(Vector2 screenPos)
    {
        if (!_isMyTurn || _turnEnded) return;

        if (GetGridFromScreen(screenPos, out GridCoords targetCoords))
        {
            Debug.Log($"<color=green>[Command] Move To: {targetCoords}</color>");
            // Step 3에서 여기에 이동 로직 추가 예정
        }
    }

    // 취소/종료 핸들러
    private void HandleCancel()
    {
        if (!_isMyTurn) return;
        Debug.Log("[PlayerController] Turn End Requested.");
        EndTurn();
    }

    // ========================================================================
    // 4. 유틸리티 (Raycast)
    // ========================================================================
    private bool GetGridFromScreen(Vector2 screenPos, out GridCoords coords)
    {
        coords = default;
        if (_mainCamera == null || _mapManager == null) return false;

        Ray ray = _mainCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayerMask))
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