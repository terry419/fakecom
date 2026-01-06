using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class TurnManager : MonoBehaviour, IInitializable
{
    [Tooltip("1초당 감소하는 TS 수치입니다.")]
    [SerializeField] private float tsDecrementPerSecond = 5f;

    // [Optimization] TS 업데이트 주기 설정 (0.1초)
    private const float TS_UPDATE_INTERVAL = 0.1f;
    private float _tsUpdateTimer = 0f;

    private List<UnitStatus> allUnits = new List<UnitStatus>();
    public UnitStatus ActiveUnit { get; private set; }
    private bool isTurnActive = false;

    public event System.Action<UnitStatus> OnTurnStarted;
    public event System.Action<List<UnitStatus>> OnTimelineUpdated;

    // [Refactor] CameraController, InputManager 의존성 삭제됨

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDisable()
    {
        // [Refactor] InputManager 구독 해제 로직 삭제됨
        if (ServiceLocator.IsRegistered<TurnManager>())
            ServiceLocator.Unregister<TurnManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // [Refactor] 의존성(Camera/Input) 가져오는 코드 삭제

        // 씬 내의 모든 유닛 수집
        allUnits = FindObjectsOfType<UnitStatus>().OrderBy(u => u.gameObject.name).ToList();
        Debug.Log($"[TurnManager] 초기화 완료. 감지된 유닛: {allUnits.Count}");

        // 초기 TS 부여 (민첩성 기반)
        foreach (var unit in allUnits)
        {
            if (unit.Agility > 0)
                unit.CurrentTS = Random.Range(20f, 40f) / unit.Agility;
            else
                unit.CurrentTS = 100f;
        }

        // [Refactor] InputManager 이벤트 구독 삭제

        isTurnActive = false;
        ActiveUnit = null;

        OnTimelineUpdated?.Invoke(allUnits);

        await UniTask.CompletedTask;
    }

    private void Update()
    {
        // 턴이 진행 중이거나 유닛이 없으면 패스
        if (isTurnActive || allUnits == null || allUnits.Count == 0) return;

        // [Optimization] 0.1초마다 로직 실행
        _tsUpdateTimer += Time.deltaTime;
        if (_tsUpdateTimer < TS_UPDATE_INTERVAL) return;

        // 경과 시간만큼 TS 감소 계산
        float tsToDecrement = tsDecrementPerSecond * _tsUpdateTimer;
        _tsUpdateTimer = 0f; // 타이머 리셋

        bool timelineChanged = false;

        foreach (var unit in allUnits)
        {
            if (unit == null || unit.IsDead) continue;

            if (unit.CurrentTS > 0)
            {
                unit.CurrentTS -= tsToDecrement;
                timelineChanged = true;
            }

            // TS가 0 이하가 되면 턴 시작
            if (unit.CurrentTS <= 0)
            {
                unit.CurrentTS = 0;
                StartTurn(unit);
                break; // 한 프레임에 한 명만 턴 시작
            }
        }

        if (timelineChanged) OnTimelineUpdated?.Invoke(allUnits);
    }

    private void StartTurn(UnitStatus unit)
    {
        isTurnActive = true;
        ActiveUnit = unit;
        ActiveUnit.ResetTurnData();

        // [Refactor] 카메라 이동 명령 삭제 (CameraController가 이벤트를 구독하여 스스로 처리함)

        ActiveUnit.OnTurnStart();

        if (ActiveUnit.IsDead)
        {
            Debug.Log($"{ActiveUnit.name}(이)가 턴 시작과 동시에 사망했습니다.");
            EndTurn();
            return;
        }

        // 이 이벤트를 통해 외부(카메라, UI 등)가 반응
        OnTurnStarted?.Invoke(ActiveUnit);
    }

    public void EndTurn()
    {
        if (!isTurnActive || ActiveUnit == null) return;

        // PathVisualizer는 여전히 필요하므로 로컬로 가져와 사용 (간접 참조)
        var pathVisualizer = ServiceLocator.Get<PathVisualizer>();
        if (pathVisualizer != null) pathVisualizer.ClearAll();

        // 다음 턴을 위한 페널티 부여
        ActiveUnit.CurrentTS += ActiveUnit.CalculateNextTurnPenalty();
        ActiveUnit.ResetTurnData();
        ActiveUnit = null;
        isTurnActive = false;

        OnTimelineUpdated?.Invoke(allUnits);
    }

    // 타임라인 UI 연출용 (기존 유지)
    public void UpdateUnitTurnDelay(UnitStatus unit, float oldTS, float newTS, bool isCrit)
    {
        StartCoroutine(ProcessTurnDelayAnimation(unit, oldTS, newTS, isCrit));
    }

    private IEnumerator ProcessTurnDelayAnimation(UnitStatus unit, float startTS, float targetTS, bool isCrit)
    {
        if (!isCrit)
        {
            float duration = 0.3f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                unit.CurrentTS = Mathf.Lerp(startTS, targetTS, t);
                OnTimelineUpdated?.Invoke(allUnits);
                yield return null;
            }
        }
        else
        {
            float difference = targetTS - startTS;
            float overshootAmount = difference * 0.5f;
            float overshootTarget = targetTS + overshootAmount;

            // Phase 1: Overshoot
            float phase1Duration = 0.2f;
            float p1Timer = 0f;
            while (p1Timer < phase1Duration)
            {
                p1Timer += Time.deltaTime;
                float t = p1Timer / phase1Duration;
                float easeT = 1f - Mathf.Pow(1f - t, 3);
                unit.CurrentTS = Mathf.Lerp(startTS, overshootTarget, easeT);
                OnTimelineUpdated?.Invoke(allUnits);
                yield return null;
            }
            unit.CurrentTS = overshootTarget;

            // Phase 2: Settle
            float phase2Duration = 0.3f;
            float p2Timer = 0f;
            while (p2Timer < phase2Duration)
            {
                p2Timer += Time.deltaTime;
                float t = p2Timer / phase2Duration;
                float smoothT = t * t * (3f - 2f * t);
                unit.CurrentTS = Mathf.Lerp(overshootTarget, targetTS, smoothT);
                OnTimelineUpdated?.Invoke(allUnits);
                yield return null;
            }
        }
        unit.CurrentTS = targetTS;
        OnTimelineUpdated?.Invoke(allUnits);
    }

    // [Refactor] HandleCameraRecenterInput 메서드 삭제됨
}