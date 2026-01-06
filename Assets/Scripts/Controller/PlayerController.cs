using Cysharp.Threading.Tasks;
using UnityEngine;

public class PlayerController : MonoBehaviour, IInitializable
{
    public Unit PossessedUnit { get; private set; }

    private PlayerInputHandler _inputHandler;
    private CameraController _cameraController;
    private InputManager _inputManager;

    private BaseAction _selectedAction;
    private UniTaskCompletionSource _turnCompletionSource;
    private bool _isMyTurn = false;

    public UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);

        var mapManager = ServiceLocator.Get<MapManager>();
        _inputManager = ServiceLocator.Get<InputManager>();

        if (_inputManager == null)
        {
            Debug.LogError("[PlayerController] InputManager not found");
            return UniTask.CompletedTask;
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

        return UniTask.CompletedTask;
    }

    public UniTask<bool> Possess(Unit unit)
    {
        if (unit == null) return UniTask.FromResult(false);
        PossessedUnit = unit;
        _cameraController?.SetTarget(unit.transform);
        SetAction(unit.GetDefaultAction());
        return UniTask.FromResult(true);
    }

    public async UniTask Unpossess()
    {
        CleanupTurn();
        PossessedUnit = null;
        await UniTask.Yield();
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;
        _isMyTurn = true;
        _turnCompletionSource = new UniTaskCompletionSource();

        if (_inputHandler != null)
        {
            _inputHandler.OnCancelRequested += OnCancelRequested;
        }

        _selectedAction?.OnSelect();
        _inputHandler.SetActive(true);

        await _turnCompletionSource.Task;
        CleanupTurn();
    }

    public void EndTurn()
    {
        if (!_isMyTurn) return;
        _turnCompletionSource?.TrySetResult();
    }

    private void CleanupTurn()
    {
        _isMyTurn = false;
        _inputHandler.SetActive(false);

        if (_inputHandler != null)
        {
            _inputHandler.OnCancelRequested -= OnCancelRequested;
        }

        _selectedAction?.OnExit();
        _selectedAction = null;
    }

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
            // [Fix] 실행 불가능한 액션(예: 이동한 스나이퍼)은 모드 전환 자체를 차단
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
                        // 아직 공격 가능함 -> 대기
                        // (Sniper가 이동했다면 공격 불가이므로 사실상 할 게 없지만, 
                        // 유저가 직접 턴 종료를 누르게 유도하거나 자동 종료 로직 추가 가능)
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