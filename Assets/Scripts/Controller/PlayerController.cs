using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

// [Update] IUnitController 인터페이스 구현 추가
public class PlayerController : MonoBehaviour, IInitializable
{
    public Unit PossessedUnit { get; private set; }

    private PlayerInputHandler _inputHandler;
    private CameraController _cameraController;
    private InputManager _inputManager;

    private BaseAction _selectedAction;
    private UniTaskCompletionSource _turnCompletionSource; // 비동기 턴 대기용 소스
    private bool _isMyTurn = false;

    public async UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);

        var mapManager = ServiceLocator.Get<MapManager>();
        _inputManager = ServiceLocator.Get<InputManager>();

        if (_inputManager == null)
        {
            Debug.LogError("[PlayerController] InputManager not found");
            return; // UniTask.CompletedTask 자동 처리
        }

        _inputHandler = gameObject.GetComponent<PlayerInputHandler>();
        if (_inputHandler == null) _inputHandler = gameObject.AddComponent<PlayerInputHandler>();
        _inputHandler.Initialize(_inputManager, mapManager);

        _cameraController = FindObjectOfType<CameraController>();

        _inputHandler.OnHoverChanged += OnHoverChanged;
        _inputHandler.OnMoveRequested += OnMoveRequested;

        if (_inputManager != null)
        {
            _inputManager.OnAbilityHotkeyPressed += OnHotkeyPressed;
        }

        await UniTask.CompletedTask;
    }

    // [New] 비동기 빙의 (IUnitController 구현)
    public async UniTask<bool> Possess(Unit unit)
    {
        if (unit == null)
        {
            Debug.LogError("[PlayerController] Cannot possess null unit.");
            return false;
        }

        PossessedUnit = unit;
        _isMyTurn = true;

        // [Option] 카메라 포커싱 (비동기 대기)
        if (_cameraController != null)
        {
            // 실제 CameraController 구현에 따라 await 방식 적용
            // await _cameraController.FocusUnit(unit);
            await UniTask.Yield(); // 임시 대기
        }

        Debug.Log($"[PlayerController] Possessed {unit.name}");
        return true;
    }

    // [New] 비동기 빙의 해제 (IUnitController 구현)
    public async UniTask Unpossess()
    {
        if (PossessedUnit == null) return;

        Debug.Log($"[PlayerController] Unpossessing {PossessedUnit.name}");

        CleanupTurn(); // 입력 비활성화 등 정리
        PossessedUnit = null;
        _isMyTurn = false;

        await UniTask.CompletedTask;
    }

    // [New] 턴 시작 로직 (비동기 대기 지원)
    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;

        _isMyTurn = true;
        // 턴 종료를 기다리기 위한 TCS 생성
        _turnCompletionSource = new UniTaskCompletionSource();

        if (_inputHandler != null)
        {
            _inputHandler.OnCancelRequested += OnCancelRequested;
            // 입력 활성화
            _inputHandler.SetActive(true);
        }

        // 기본 액션 선택
        _selectedAction?.OnSelect();
        if (_selectedAction == null) SetAction(PossessedUnit.GetDefaultAction());

        Debug.Log("[PlayerController] Turn Started. Waiting for input...");

        // [Wait] EndTurn()이 호출될 때까지 여기서 대기합니다.
        await _turnCompletionSource.Task;

        CleanupTurn();
    }

    // [New] 턴 종료 신호 (외부 혹은 내부 로직에서 호출)
    public void OnTurnEnd() => EndTurn(); // 인터페이스 구현용

    public void EndTurn()
    {
        if (!_isMyTurn) return;
        Debug.Log("[PlayerController] EndTurn called.");
        _turnCompletionSource?.TrySetResult(); // 대기 중인 OnTurnStart를 해제
    }

    private void CleanupTurn()
    {
        _isMyTurn = false;

        if (_inputHandler != null)
        {
            _inputHandler.SetActive(false);
            _inputHandler.OnCancelRequested -= OnCancelRequested;
        }

        _selectedAction?.OnExit();
        _selectedAction = null;
    }

    // --- 기존 로직 (Action Handling) 유지 ---

    public void SetAction(BaseAction newAction)
    {
        if (_selectedAction == newAction) return;

        if (_selectedAction != null && _selectedAction.State == ActionState.Running)
        {
            _selectedAction.Cancel();
        }

        _selectedAction?.OnExit();
        _selectedAction = newAction;

        Debug.Log($"<color=cyan>[Controller] Mode Switched: {(_selectedAction != null ? _selectedAction.GetActionName() : "None")}</color>");

        if (_isMyTurn)
        {
            _selectedAction?.OnSelect();
        }
    }

    private void OnHotkeyPressed(int keyIndex)
    {
        if (!_isMyTurn || PossessedUnit == null) return;
        if (_selectedAction != null && _selectedAction.State == ActionState.Running) return;

        BaseAction targetAction = null;

        switch (keyIndex)
        {
            case 1:
                targetAction = PossessedUnit.GetAttackAction();
                break;
        }

        if (targetAction != null)
        {
            if (!targetAction.CanExecute())
            {
                Debug.LogWarning($"[Controller] Cannot switch to {targetAction.GetActionName()}: {targetAction.GetBlockReason()}");
                return;
            }

            if (_selectedAction == targetAction)
                SetAction(PossessedUnit.GetDefaultAction());
            else
                SetAction(targetAction);
        }
    }

    private void OnCancelRequested()
    {
        if (_selectedAction?.State == ActionState.Running)
        {
            _selectedAction.Cancel();
            return;
        }

        if (_selectedAction != PossessedUnit.GetDefaultAction())
        {
            SetAction(PossessedUnit.GetDefaultAction());
        }
    }

    private void OnHoverChanged(GridCoords target)
    {
        if (!_isMyTurn || _selectedAction == null) return;
        _selectedAction.OnUpdate(target);
    }

    private void OnMoveRequested(GridCoords target)
    {
        ExecuteAction(target).Forget();
    }

    // [Restore] 클래스별 분기 로직 및 Action 결과 처리 복구
    private async UniTaskVoid ExecuteAction(GridCoords target)
    {
        if (!_isMyTurn || _selectedAction == null) return;

        var result = await _selectedAction.ExecuteAsync(target);

        if (result.Success)
        {
            // 공격 완료 후 처리
            if (_selectedAction is AttackAction)
            {
                PossessedUnit.MarkAsAttacked();

                // Scout: Hit & Run (이동 모드 복귀)
                if (PossessedUnit.ClassType == ClassType.Scout)
                {
                    Debug.Log("[Controller] Scout Attack Complete. Moving to Move State.");
                    SetAction(PossessedUnit.GetDefaultAction());
                }
                // Assault, Sniper: 턴 종료
                else
                {
                    Debug.Log($"[Controller] {PossessedUnit.ClassType} Attack Complete. Turn End.");
                    EndTurn();
                }
            }
            // 이동 완료 후 처리
            else
            {
                if (PossessedUnit.CurrentMobility > 0)
                {
                    // 재이동 가능 시각화 갱신
                    if (_selectedAction == PossessedUnit.GetDefaultAction())
                    {
                        _selectedAction.OnExit();
                        _selectedAction.OnSelect();
                    }
                    else
                    {
                        SetAction(PossessedUnit.GetDefaultAction());
                    }
                }
                else
                {
                    // 이동력 소진. 공격 기회가 없으면 종료, 있으면 대기.
                    if (!PossessedUnit.HasAttacked)
                    {
                        Debug.Log("[Controller] Mobility exhausted. Waiting for action.");
                    }
                    else
                    {
                        EndTurn();
                    }
                }
            }
        }
        else if (result.Cancelled)
        {
            // 취소됨
        }
        else
        {
            Debug.LogWarning($"[Controller] Action Failed: {result.ErrorMessage}");
        }
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.OnHoverChanged -= OnHoverChanged;
            _inputHandler.OnMoveRequested -= OnMoveRequested;
            _inputHandler.OnCancelRequested -= OnCancelRequested;
        }

        if (_inputManager != null)
        {
            _inputManager.OnAbilityHotkeyPressed -= OnHotkeyPressed;
        }

        _turnCompletionSource?.TrySetResult();
    }
}