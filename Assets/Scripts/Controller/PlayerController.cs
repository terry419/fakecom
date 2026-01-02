using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class PlayerController : MonoBehaviour, IUnitController
{
    // ========================================================================
    // 1. 상태 및 설정
    // ========================================================================
    public Unit PossessedUnit { get; private set; }

    private bool _isMyTurn = false;
    private bool _turnEnded = false; // [Fix 4] 명시적 플래그
    private UniTaskCompletionSource _turnCompletionSource;

    // [Cache]
    private Camera _mainCamera;
    private MapManager _mapManager;

    // [Fix 3] 레이어 마스크 (Inspector에서 설정)
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private float _turnTimeoutSeconds = 60f; // [Fix 2] 타임아웃 설정

    private void Awake()
    {
        _mainCamera = Camera.main;
        // Ground 레이어가 설정 안 되어 있다면 기본값 세팅 (권장: Inspector 설정)
        if (_groundLayerMask == 0) _groundLayerMask = LayerMask.GetMask("Default", "Ground", "Tile");
    }

    // ========================================================================
    // 2. IUnitController 구현 (빙의 프로토콜)
    // ========================================================================
    public void Possess(Unit unit)
    {
        PossessedUnit = unit;

        // [Fix 1] 의존성 획득 시점을 실제 빙의 시점으로 지연
        if (ServiceLocator.TryGet(out MapManager map))
        {
            _mapManager = map;
        }
        else
        {
            Debug.LogError($"[{nameof(PlayerController)}] Failed to locate MapManager during Possess.");
        }

        Debug.Log($"[{nameof(PlayerController)}] Possessed Unit: {unit.name}");
    }

    public void Unpossess()
    {
        PossessedUnit = null;
        _isMyTurn = false;
        _mapManager = null; // 참조 해제
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;

        Debug.Log($"<color=cyan>[{nameof(PlayerController)}] Turn Start! (Timeout: {_turnTimeoutSeconds}s)</color>");

        _isMyTurn = true;
        _turnEnded = false;
        _turnCompletionSource = new UniTaskCompletionSource();

        // [Fix 2] 타임아웃 로직 추가
        var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(_turnTimeoutSeconds));
        var turnTask = _turnCompletionSource.Task;

        var result = await UniTask.WhenAny(turnTask, timeoutTask);

        if (result == 1) // 타임아웃 발생 인덱스
        {
            Debug.LogWarning($"[{nameof(PlayerController)}] Turn Timed Out!");
            EndTurn(); // 강제 종료 처리
        }
    }

    public void OnTurnEnd()
    {
        _isMyTurn = false;
        Debug.Log($"<color=gray>[{nameof(PlayerController)}] Turn End.</color>");
    }

    // ========================================================================
    // 3. 입력 처리
    // ========================================================================
    private void Update()
    {
        // 턴이 아니거나, 이미 종료 절차 중이면 무시
        if (!_isMyTurn || _turnEnded || PossessedUnit == null) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            EndTurn();
            return;
        }

        if (Input.GetMouseButtonDown(1)) // Right Click
        {
            HandleMoveInput();
        }
    }

    private void ConnectInput()
    {
        if (_inputManager == null) return;

        // [Fix 1 대응] 변경된 이벤트 이름 사용 (OnSelectInput, OnCommandInput...)
        _inputManager.OnSelectInput -= HandleSelect;
        _inputManager.OnCommandInput -= HandleCommand;
        _inputManager.OnCancelInput -= HandleCancel;

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

    private void HandleMoveInput()
    {
        if (_mapManager == null || _mainCamera == null) return;

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // [Fix 3] LayerMask 적용
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _groundLayerMask))
        {
            GridCoords targetCoords = GridUtils.WorldToGrid(hit.point);

            // [Fix 5] GridUtils 왕복 테스트 겸용 검증
            // (실제로는 여기서 할 필요 없지만, 개발 단계에서 검증용)
            // Vector3 checkPos = GridUtils.GridToWorld(targetCoords);
            // Debug.DrawLine(hit.point, checkPos, Color.red, 1f);

            if (_mapManager.HasTile(targetCoords))
            {
                Debug.Log($"[{nameof(PlayerController)}] Request Move: {PossessedUnit.Coordinate} -> {targetCoords}");
                // PossessedUnit.MoveTo(targetCoords);
            }
        }
    }

    public void EndTurn()
    {
        if (_turnEnded) return;
        _turnEnded = true;

        if (_turnCompletionSource != null && _turnCompletionSource.GetStatus(0) == UniTaskStatus.Pending)
        {
            _turnCompletionSource.TrySetResult();
        }
    }
}