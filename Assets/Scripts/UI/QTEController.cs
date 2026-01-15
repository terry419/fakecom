using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class QTEController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private QTEUIController _uiController;
    [SerializeField] private ActionModuleSO _defaultModule;

    private InputManager _inputManager;
    private ActionModuleSO _currentModule;

    // 상태 변수
    private bool _isActive = false;
    private bool _qteEnded = false;
    private bool _inputStarted = false;
    private float _holdTimer = 0f;

    private ZonesContainer _activeZones;
    private UniTaskCompletionSource<QTEResult> _qteCompletionSource;

    private void Start()
    {
        _inputManager = ServiceLocator.Get<InputManager>();

        if (_uiController == null)
            _uiController = GetComponent<QTEUIController>();
    }

    // ========================================================================
    // 1. Lifecycle
    // ========================================================================
    public async UniTask<QTEResult> StartQTEAsync(float hitChance, float critChance, ActionModuleSO module)
    {
        if (_isActive) return QTEResult.None;

        // 초기화
        _currentModule = module != null ? module : _defaultModule;
        _isActive = true;
        _qteEnded = false;
        _inputStarted = false;
        _holdTimer = 0f;

        // 계산 및 UI 설정
        CalculateAndSetupZones(hitChance, critChance);

        // UI 켜기
        _uiController.StartQTE();

        // 입력 제어권 확보
        if (_inputManager != null)
        {
            _inputManager.SetQTEContext(true);
            _inputManager.OnQTEInput += OnInputReceived;
        }

        // 결과 대기 (Task 경쟁)
        _qteCompletionSource = new UniTaskCompletionSource<QTEResult>();

        var inputTask = _qteCompletionSource.Task;
        var timeoutTask = UniTask.Delay(TimeSpan.FromSeconds(_currentModule.Timeout))
                                 .ContinueWith(() => QTEResult.Timeout);

        // (index, result1, result2) 튜플 반환
        var (winIndex, inputRes, timeoutRes) = await UniTask.WhenAny(inputTask, timeoutTask);

        // 경쟁 종료 즉시 플래그 설정
        _qteEnded = true;

        QTEGrade finalGrade = QTEGrade.Miss;

        // winIndex 0: Input, 1: Timeout
        if (winIndex == 1)
        {
            Debug.Log("[QTE] Time Out!");
            finalGrade = QTEGrade.Miss;
        }
        else
        {
            // 입력 승리 시 판정 수행
            float val = GetCurrentPingPongValue();
            finalGrade = QTEMath.EvaluateResult(_activeZones, val);
        }

        // 결과 연출
        _uiController.ShowVerdict(finalGrade);

        // 결과 변환
        QTEResult finalResult = finalGrade switch
        {
            QTEGrade.Miss => QTEResult.Miss,
            QTEGrade.Graze => QTEResult.Success,
            QTEGrade.Hit => QTEResult.Success,
            QTEGrade.Critical => QTEResult.Success,
            _ => QTEResult.Miss
        };

        // 뒷정리
        Cleanup();

        await UniTask.Delay(500);
        _uiController.EndQTE();

        return finalResult;
    }

    private void Cleanup()
    {
        _isActive = false;
        _inputStarted = false;

        if (_inputManager != null)
        {
            _inputManager.OnQTEInput -= OnInputReceived;
            _inputManager.SetQTEContext(false);
        }
    }

    private void CalculateAndSetupZones(float hit, float crit)
    {
        _activeZones = _currentModule.CalculateZones(hit, crit);
        _uiController.SetupZones(_activeZones);
    }

    // ========================================================================
    // 2. Core Loop
    // ========================================================================
    private void Update()
    {
        if (!_isActive) return;

        _holdTimer += Time.deltaTime * _currentModule.ScrollSpeed;
        _uiController.UpdateNeedle(GetCurrentPingPongValue());
    }

    private float GetCurrentPingPongValue()
    {
        if (_currentModule.IsPingPong)
            return Mathf.PingPong(_holdTimer, 1.0f);
        else
            return Mathf.Repeat(_holdTimer, 1.0f);
    }

    // ========================================================================
    // 3. Input Handling
    // ========================================================================
    private void OnInputReceived(bool isPressed)
    {
        if (!_isActive || _qteEnded) return;

        if (isPressed && !_inputStarted)
        {
            _inputStarted = true;
        }
        else if (!isPressed && _inputStarted)
        {
            _inputStarted = false;
            EvaluateAndFinish();
        }
    }

    private void EvaluateAndFinish()
    {
        // 중복 체크
        if (_qteEnded) return;

        // 단순 신호 전달
        _qteCompletionSource.TrySetResult(QTEResult.Success);
    }

    // ========================================================================
    // 검증용 (UniTaskVoid 패턴 사용)
    // ========================================================================
    [ContextMenu("Debug/Start QTE (Hit:50, Crit:20)")]
    private void DebugStartQTE()
    {
        if (_defaultModule == null)
        {
            Debug.LogError("인스펙터에 Default Module을 할당해주세요.");
            return;
        }

        // Fire and Forget
        DebugStartQTEAsync().Forget();
    }

    private async UniTaskVoid DebugStartQTEAsync()
    {
        Debug.Log("[검증 시작] QTE 실행");

        // await를 사용하여 결과를 안전하게 받아옴
        QTEResult result = await StartQTEAsync(50f, 20f, _defaultModule);

        Debug.Log($"[검증 종료] 최종 반환값: {result}");
    }
}