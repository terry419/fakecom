using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerTurnState : BattleStateBase
{
    public override BattleState StateID => BattleState.PlayerTurn;
    private readonly BattleContext _context;

    // [개선 3] 메모리 누수 방지를 위해 관리되는 소스
    private UniTaskCompletionSource _turnEndSource;

    public PlayerTurnState(BattleContext context)
    {
        _context = context;
    }

    public override async UniTask Enter(StatePayload payload, CancellationToken cancellationToken)
    {
        Debug.Log($"<color=magenta>[TRACER] 1. PlayerTurnState 진입 성공! (ActiveUnit: {_context.Turn.ActiveUnit?.name})</color>");
        // 1. 데이터 검증 및 GetComponent 최적화
        var activeUnitStatus = _context.Turn.ActiveUnit;
        if (activeUnitStatus == null)
        {
            Debug.LogError("[PlayerTurnState] ActiveUnit is null. Reverting to Waiting.");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        // [개선 4] GetComponent 호출 최적화 (ActiveUnit이 변경될 때만 호출됨)
        // UnitStatus와 Unit 컴포넌트가 같은 오브젝트에 있다고 가정
        if (!activeUnitStatus.TryGetComponent<Unit>(out var unitComponent))
        {
            Debug.LogError($"[PlayerTurnState] '{activeUnitStatus.name}' has no Unit component!");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        Debug.Log($"[PlayerTurnState] Start Turn: {activeUnitStatus.name}");

        // 2. 매니저 참조
        var playerController = ServiceLocator.Get<PlayerController>();
        var inputManager = ServiceLocator.Get<InputManager>();

        if (playerController == null || inputManager == null)
        {
            Debug.LogError("[PlayerTurnState] Missing Managers.");
            RequestTransition(BattleState.TurnWaiting, null);
            return;
        }

        // 3. [개선 1] Race Condition 방지: await 전에 이벤트 리스너 설정
        _turnEndSource = new UniTaskCompletionSource();
        inputManager.OnTurnEndInvoked += OnTurnEndSignal;

        try
        {
            // 4. [개선 1 & 5] Possess 성공 여부 확인 및 bool 반환 처리
            // (PlayerController.Possess가 UniTask<bool>을 반환하도록 수정되어야 함)
            bool isPossessSuccessful = await playerController.Possess(unitComponent);

            if (!isPossessSuccessful)
            {
                Debug.LogError("[PlayerTurnState] Failed to Possess unit.");
                RequestTransition(BattleState.TurnWaiting, null);
                return;
            }

            // 5. 턴 종료 대기 (CancellationToken 연동)
            await _turnEndSource.Task.AttachExternalCancellation(cancellationToken);
        }
        catch (System.OperationCanceledException)
        {
            // 상태 전환이나 게임 종료로 인한 취소는 정상 흐름
        }
        finally
        {
            // [개선 1] 이벤트 구독 해제 보장
            if (inputManager != null)
            {
                inputManager.OnTurnEndInvoked -= OnTurnEndSignal;
            }
        }
    }

    private void OnTurnEndSignal()
    {
        // 이미 완료된 상태가 아니라면 결과 설정
        if (_turnEndSource != null && !_turnEndSource.Task.Status.IsCompleted())
        {
            _turnEndSource.TrySetResult();
        }
    }

    public override async UniTask Exit(CancellationToken cancellationToken)
    {
        // [개선 3] CompletionSource 명시적 정리 (메모리 누수 방지)
        if (_turnEndSource != null)
        {
            _turnEndSource.TrySetCanceled(); // 남은 Task가 있다면 취소 처리
            _turnEndSource = null;
        }

        var playerController = ServiceLocator.Get<PlayerController>();

        // [개선 2] Unpossess 완료 대기 (정리 보장)
        if (playerController != null)
        {
            await playerController.Unpossess();
        }

        // 턴 종료 처리
        if (_context.Turn != null)
        {
            _context.Turn.EndTurn();
        }

        Debug.Log("[PlayerTurnState] Turn Ended. Cleaning up.");

        // 다음 상태로 전환
        RequestTransition(BattleState.TurnWaiting, null);
    }

    public override void Update()
    {
        // 1. 입력 감지
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"<color=yellow>[DEBUG_INPUT] Click detected. Frame: {Time.frameCount}</color>");

            // 2. Raycast 수행
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // LayerMask 없이 모든 충돌체 검사 (원인 파악용)
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                GameObject hitObj = hit.collider.gameObject;
                string layerName = LayerMask.LayerToName(hitObj.layer);

                Debug.Log($"<color=yellow>[DEBUG_RAY] Hit Object: {hitObj.name}</color>\n" +
                          $"- Layer: {layerName} (Index: {hitObj.layer})\n" +
                          $"- World Point: {hit.point}");

                // 3. MapManager를 통해 좌표 변환 및 타일 검증
                var mapManager = ServiceLocator.Get<MapManager>();
                if (mapManager != null)
                {
                    // [확인됨] MapManager.WorldToGrid 반환값은 GridCoords임
                    GridCoords coords = mapManager.WorldToGrid(hit.point);

                    // [확인됨] GridCoords.ToString() 오버라이드 존재하므로 바로 로그 출력 가능
                    Debug.Log($"<color=cyan>[DEBUG_GRID] Converted Coords: {coords}</color>");

                    // [확인됨] MapManager.GetTile() 존재함
                    var tile = mapManager.GetTile(coords);

                    if (tile != null)
                    {
                        // [확인됨] MapManager.TryGetRandomTileByTag에서 tile.IsWalkable 사용 확인됨
                        Debug.Log($"<color=green>[DEBUG_TILE] Tile Found!</color>\n" +
                                  $"- FloorID: {tile.FloorID}\n" +
                                  $"- IsWalkable: {tile.IsWalkable}");

                        // 현재 턴 유닛 정보 로그 (Context 사용)
                        if (_context != null && _context.Turn != null && _context.Turn.ActiveUnit != null)
                        {
                            Debug.Log($"[DEBUG_CONTEXT] Current ActiveUnit: {_context.Turn.ActiveUnit.name}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"<color=red>[DEBUG_TILE] No Tile found at {coords}. (Grid 범위 밖이거나 데이터 없음)</color>");
                    }
                }
                else
                {
                    Debug.LogError("[DEBUG_ERROR] MapManager is NULL via ServiceLocator.");
                }
            }
            else
            {
                Debug.LogError("<color=red>[DEBUG_RAY] Raycast hit NOTHING. (Camera settings or Missing Colliders)</color>");
            }
        }
    }
}