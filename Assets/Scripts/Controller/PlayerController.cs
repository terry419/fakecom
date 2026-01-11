using Cysharp.Threading.Tasks;
using UnityEngine;
using System;

public class PlayerController : MonoBehaviour, IInitializable
{
    public Unit PossessedUnit { get; private set; }

    private PlayerInputHandler _inputHandler;
    private CameraController _cameraController;
    private InputManager _inputManager;

    private BaseAction _selectedAction;
    private UniTaskCompletionSource _turnCompletionSource;
    private bool _isMyTurn = false;

    public async UniTask Initialize(InitializationContext context)
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
        var mapManager = ServiceLocator.Get<MapManager>();
        _inputManager = ServiceLocator.Get<InputManager>();

        if (_inputManager == null)
        {
            Debug.LogError("[PlayerController] InputManager not found");
            return;
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

    public async UniTask<bool> Possess(Unit unit)
    {
        if (unit == null) return false;
        PossessedUnit = unit;
        _isMyTurn = true;

        if (_cameraController != null) await UniTask.Yield();

        Debug.Log($"[PlayerController] Possessed {unit.name}");
        return true;
    }

    public async UniTask Unpossess()
    {
        if (PossessedUnit == null) return;
        CleanupTurn();
        PossessedUnit = null;
        _isMyTurn = false;
        await UniTask.CompletedTask;
    }

    public async UniTask OnTurnStart()
    {
        if (PossessedUnit == null) return;

        _isMyTurn = true;
        _turnCompletionSource = new UniTaskCompletionSource();

        if (_inputHandler != null)
        {
            _inputHandler.OnCancelRequested += OnCancelRequested;
            _inputHandler.SetActive(true);
        }

        _selectedAction?.OnSelect();
        if (_selectedAction == null) SetAction(PossessedUnit.GetDefaultAction());

        Debug.Log("[PlayerController] Turn Started. Waiting for input...");
        await _turnCompletionSource.Task;

        CleanupTurn();
    }

    public void OnTurnEnd() => EndTurn();

    public void EndTurn()
    {
        if (!_isMyTurn) return;
        Debug.Log("[PlayerController] EndTurn called.");
        _turnCompletionSource?.TrySetResult();
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

    public void SetAction(BaseAction newAction)
    {
        // [Optimization] 같은 액션이면 초기화 로직을 건너뜀
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

    // [Step 3 Complete] 로직 이동 완료. Action의 결과를 받아 처리만 수행
    private async UniTaskVoid ExecuteAction(GridCoords target)
    {
        if (!_isMyTurn || _selectedAction == null) return;

        // Action 실행 및 결과 대기
        var result = await _selectedAction.ExecuteAsync(target);

        if (result.Success)
        {
            // 결과(Consequence)에 따른 후속 처리 위임
            HandleConsequence(result.Consequence);
        }
        else if (result.Cancelled)
        {
            // 취소됨 (로그는 BaseAction에서 처리됨)
        }
        else
        {
            Debug.LogWarning($"[Controller] Action Failed: {result.ErrorMessage}");
        }
    }

    private void HandleConsequence(ActionConsequence consequence)
    {
        switch (consequence)
        {
            case ActionConsequence.EndTurn:
                // 턴 종료
                EndTurn();
                break;

            case ActionConsequence.SwitchToDefaultAction:
                // Default Action(Move)으로 복귀
                SetAction(PossessedUnit.GetDefaultAction());

                // [Defensive Coding] SetAction의 Early Return으로 인해 
                // 동일 액션(Move -> Move) 전환 시 시각화(파란 타일 등)가 갱신되지 않는 문제를 방지
                _selectedAction?.OnSelect();
                break;

            case ActionConsequence.WaitForInput:
                // 추가 조치 없음 (대기)
                break;

            case ActionConsequence.None:
            default:
                // 아무 조치 없음
                break;
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