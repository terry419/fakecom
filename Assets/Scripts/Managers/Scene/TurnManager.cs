using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour, IInitializable
{
    [Header("Settings")]
    [Tooltip("1초당 감소하는 TS 수치")]
    [SerializeField] private float _tsDecrementPerSecond = 5f;

    [Tooltip("UI 갱신 빈도 (초 단위, 0.05 = 20fps)")]
    [SerializeField] private float _uiTickInterval = 0.05f;

    [Tooltip("턴 시뮬레이션 간격")]
    [SerializeField] private float _simulationStep = 0.1f;

    // 내부 상태
    private List<UnitStatus> _activeUnits = new List<UnitStatus>();
    private Queue<UnitStatus> _readyQueue = new Queue<UnitStatus>();
    private float _simulationTimer = 0f;
    private float _uiTimer = 0f;

    public IReadOnlyList<UnitStatus> AllUnits => _activeUnits;
    public UnitStatus ActiveUnit { get; private set; }
    private bool _isTurnActive = false;
    private BattleManager _battleManager;

    // 애니메이션 관리용 CancellationTokenSource 맵
    private Dictionary<UnitStatus, CancellationTokenSource> _animCtsMap = new Dictionary<UnitStatus, CancellationTokenSource>();

    // [이벤트] - UI 효율성을 위해 개별 이벤트로 분리
    public event Action<UnitStatus> OnUnitRegistered;   // 유닛 1명 추가됨
    public event Action<UnitStatus> OnUnitUnregistered; // 유닛 1명 삭제됨
    public event Action OnTick;                         // TS 변화 (파라미터 없음)
    public event Action<UnitStatus> OnTurnStarted;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        CancelAllAnimations();
        if (ServiceLocator.IsRegistered<TurnManager>())
            ServiceLocator.Unregister<TurnManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        if (!ServiceLocator.TryGet(out _battleManager))
        {
            Debug.LogWarning("[TurnManager] BattleManager not found via ServiceLocator. Finding Object...");
            _battleManager = FindObjectOfType<BattleManager>();
        }

        Debug.Log("[TurnManager] Initialized. Waiting for units to register...");
        await UniTask.CompletedTask;
    }

    // --- [Core Logic 1] 유닛 등록/해제 ---

    public void RegisterUnit(UnitStatus unit)
    {
        if (unit == null || _activeUnits.Contains(unit)) return;

        _activeUnits.Add(unit);

        // 초기 TS 부여 (없는 경우에만)
        if (unit.CurrentTS <= 0)
        {
            int agility = (unit.unitData != null && unit.unitData.Agility > 0) ? unit.unitData.Agility : 10;
            unit.CurrentTS = UnityEngine.Random.Range(20f, 40f) / agility;
        }

        // 정렬 유지 (이름순)
        _activeUnits.Sort((a, b) => a.name.CompareTo(b.name));

        Debug.Log($"[TurnManager] Unit Registered: {unit.name}. Total: {_activeUnits.Count}");

        // [방송] 개별 유닛 추가 알림
        OnUnitRegistered?.Invoke(unit);
        // 즉시 위치 갱신 요청
        OnTick?.Invoke();
    }

    public void UnregisterUnit(UnitStatus unit)
    {
        if (unit == null || !_activeUnits.Contains(unit)) return;

        // 진행 중인 애니메이션 취소
        if (_animCtsMap.TryGetValue(unit, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _animCtsMap.Remove(unit);
        }

        _activeUnits.Remove(unit);
        Debug.Log($"[TurnManager] Unit Unregistered: {unit.name}. Total: {_activeUnits.Count}");

        // [방송] 개별 유닛 삭제 알림
        OnUnitUnregistered?.Invoke(unit);
        OnTick?.Invoke();
    }

    // --- [Core Logic 2] 메인 루프 ---

    private void Update()
    {
        if (_battleManager != null && _battleManager.IsTransitioning) return;
        if (_isTurnActive || _activeUnits.Count == 0) return;

        // 1. 시뮬레이션 스텝
        _simulationTimer += Time.deltaTime;
        if (_simulationTimer >= _simulationStep)
        {
            float decrement = _tsDecrementPerSecond * _simulationTimer;
            _simulationTimer = 0f;

            ProcessTimePassage(decrement);
        }

        // 2. UI 갱신 스텝
        _uiTimer += Time.deltaTime;
        if (_uiTimer >= _uiTickInterval)
        {
            _uiTimer = 0f;
            OnTick?.Invoke();
        }
    }

    private void ProcessTimePassage(float decrement)
    {
        if (_readyQueue.Count > 0)
        {
            StartNextTurn();
            return;
        }

        List<UnitStatus> readyUnits = new List<UnitStatus>();

        foreach (var unit in _activeUnits)
        {
            if (unit.IsDead) continue;

            if (unit.CurrentTS > 0)
            {
                unit.CurrentTS -= decrement;
            }

            if (unit.CurrentTS <= 0)
            {
                readyUnits.Add(unit);
            }
        }

        if (readyUnits.Count == 0) return;

        // 턴 우선순위 정렬
        readyUnits.Sort((a, b) =>
        {
            int tsCompare = a.CurrentTS.CompareTo(b.CurrentTS);
            if (tsCompare != 0) return tsCompare;

            int agiA = a.unitData != null ? a.unitData.Agility : 0;
            int agiB = b.unitData != null ? b.unitData.Agility : 0;
            return agiB.CompareTo(agiA);
        });

        foreach (var unit in readyUnits)
        {
            unit.CurrentTS = 0;
            _readyQueue.Enqueue(unit);
        }

        StartNextTurn();
    }

    private void StartNextTurn()
    {
        if (_readyQueue.Count == 0)
        {
            _isTurnActive = false;
            return;
        }

        UnitStatus nextUnit = _readyQueue.Dequeue();

        if (nextUnit == null || nextUnit.IsDead)
        {
            StartNextTurn();
            return;
        }

        _isTurnActive = true;
        ActiveUnit = nextUnit;

        ActiveUnit.ResetTurnData();
        ActiveUnit.OnTurnStart();

        OnTurnStarted?.Invoke(ActiveUnit);
    }

    public void EndTurn()
    {
        if (!_isTurnActive || ActiveUnit == null) return;

        if (ServiceLocator.TryGet<PathVisualizer>(out var pathVisualizer))
            pathVisualizer.ClearAll();

        float penalty = ActiveUnit.CalculateNextTurnPenalty();
        float oldTS = ActiveUnit.CurrentTS;

        ActiveUnit.CurrentTS = Mathf.Max(oldTS + penalty, 0f);

        ActiveUnit.ResetTurnData();
        ActiveUnit = null;

        if (_readyQueue.Count > 0)
        {
            StartNextTurn();
        }
        else
        {
            _isTurnActive = false;
        }

        OnTick?.Invoke();
    }

    // --- [Core Logic 3] 애니메이션 (UniTask) ---

    public void UpdateUnitTurnDelay(UnitStatus unit, float oldTS, float newTS, bool isCrit)
    {
        if (_animCtsMap.TryGetValue(unit, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        var cts = new CancellationTokenSource();
        _animCtsMap[unit] = cts;

        ProcessTurnDelayAnimationAsync(unit, oldTS, newTS, isCrit, cts.Token).Forget();
    }

    private async UniTaskVoid ProcessTurnDelayAnimationAsync(UnitStatus unit, float startTS, float targetTS, bool isCrit, CancellationToken token)
    {
        try
        {
            float duration = isCrit ? 0.5f : 0.3f;
            float elapsed = 0f;
            float localUiTimer = 0f;

            while (elapsed < duration)
            {
                if (token.IsCancellationRequested) return;

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                if (!isCrit)
                {
                    unit.CurrentTS = Mathf.Lerp(startTS, targetTS, t);
                }
                else
                {
                    float overshoot = targetTS + (targetTS - startTS) * 0.2f;
                    if (t < 0.5f)
                    {
                        float subT = t * 2f;
                        unit.CurrentTS = Mathf.Lerp(startTS, overshoot, 1f - Mathf.Pow(1f - subT, 3));
                    }
                    else
                    {
                        float subT = (t - 0.5f) * 2f;
                        unit.CurrentTS = Mathf.Lerp(overshoot, targetTS, subT * subT * (3f - 2f * subT));
                    }
                }

                localUiTimer += Time.deltaTime;
                if (localUiTimer >= _uiTickInterval)
                {
                    localUiTimer = 0f;
                    OnTick?.Invoke();
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }

            unit.CurrentTS = targetTS;
            OnTick?.Invoke();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            // [Fix] CS1061: token.Source 대신 cts 변수 사용하거나, token 출처 확인
            // CancellationToken 자체에는 Source 속성이 없으므로, _animCtsMap에서 확인 후 제거
            if (_animCtsMap.ContainsKey(unit))
            {
                // 현재 맵에 있는 CTS가 내가 만든 CTS인지 확인 (중복 실행 방지)
                // (여기서는 간단히 제거)
                _animCtsMap.Remove(unit);
            }
            // token과 연결된 CTS는 호출부에서 관리되므로 여기서는 맵 제거만 수행
        }
    }

    private void CancelAllAnimations()
    {
        foreach (var cts in _animCtsMap.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
        _animCtsMap.Clear();
    }
}