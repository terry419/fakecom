using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks; // UniTask 사용을 위해 필수

// [변경] IInitializable 인터페이스를 추가하여 SceneInitializer의 제어를 받도록 함
public class TurnManager : MonoBehaviour, IInitializable
{
    [Tooltip("1초당 감소하는 TS 수치입니다.")]
    [SerializeField] private float tsDecrementPerSecond = 5f;

    private List<UnitStatus> allUnits = new List<UnitStatus>();
    public UnitStatus ActiveUnit { get; private set; }
    private bool isTurnActive = false;

    public event System.Action<UnitStatus> OnTurnStarted;
    public event System.Action<List<UnitStatus>> OnTimelineUpdated;

    private CameraController cameraController;
    private InputManager inputManager;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    // [변경] Start() 제거 -> Initialize()로 로직 이동
    // SceneInitializer가 UnitManager 초기화(유닛 생성)를 마친 뒤 이 함수를 호출합니다.
    public async UniTask Initialize(InitializationContext context)
    {
        cameraController = ServiceLocator.Get<CameraController>();
        inputManager = ServiceLocator.Get<InputManager>();

        // [핵심] 이 시점에는 UnitManager가 유닛 스폰을 완료했으므로 유닛을 찾을 수 있습니다.
        allUnits = FindObjectsOfType<UnitStatus>().OrderBy(u => u.gameObject.name).ToList();
        Debug.Log($"[TurnManager] 초기화 완료. 감지된 유닛 수: {allUnits.Count}");

        foreach (var unit in allUnits)
        {
            if (unit.Agility > 0)
                unit.CurrentTS = Random.Range(20f, 40f) / unit.Agility;
            else
                unit.CurrentTS = 100f; // 안전장치
        }

        // InputManager 이벤트 구독 (InputManager가 이미 초기화되었으므로 안전)
        if (inputManager != null)
        {
            inputManager.OnCameraRecenter += HandleCameraRecenterInput;
        }

        isTurnActive = false;
        ActiveUnit = null;

        OnTimelineUpdated?.Invoke(allUnits);

        await UniTask.CompletedTask;
    }

    // [변경] OnEnable/Disable에서 중복 구독 방지 및 SceneManager 의존성 제거
    private void OnDisable()
    {
        if (inputManager != null)
        {
            inputManager.OnCameraRecenter -= HandleCameraRecenterInput;
        }

        if (ServiceLocator.IsRegistered<TurnManager>())
            ServiceLocator.Unregister<TurnManager>(ManagerScope.Scene);
    }

    private void Update()
    {
        if (!isTurnActive)
        {
            if (allUnits == null || allUnits.Count == 0) return;

            bool timelineChanged = false;
            float tsToDecrement = tsDecrementPerSecond * Time.deltaTime;

            foreach (var unit in allUnits)
            {
                if (unit == null || unit.IsDead) continue;

                if (unit.CurrentTS > 0)
                {
                    unit.CurrentTS -= tsToDecrement;
                    timelineChanged = true;
                }

                if (unit.CurrentTS <= 0)
                {
                    unit.CurrentTS = 0;
                    StartTurn(unit);
                    break;
                }
            }

            if (timelineChanged) OnTimelineUpdated?.Invoke(allUnits);
        }
    }

    private void StartTurn(UnitStatus unit)
    {
        isTurnActive = true;
        ActiveUnit = unit;
        ActiveUnit.ResetTurnData();

        if (cameraController != null)
        {
            cameraController.SetTarget(ActiveUnit.transform, false);
        }
        else
        {
            Debug.LogError($"<color=red>[DEBUG_TM] 치명적 오류: CameraController가 Null입니다!</color>");
        }

        ActiveUnit.OnTurnStart();

        if (ActiveUnit.IsDead)
        {
            Debug.Log($"{ActiveUnit.name}은(는) 턴 시작과 동시에 사망했습니다.");
            EndTurn();
            return;
        }

        OnTurnStarted?.Invoke(ActiveUnit);
    }

    public void EndTurn()
    {
        if (!isTurnActive || ActiveUnit == null) return;

        var pathVisualizer = ServiceLocator.Get<PathVisualizer>();
        if (pathVisualizer != null) pathVisualizer.ClearAll();

        ActiveUnit.CurrentTS += ActiveUnit.CalculateNextTurnPenalty();
        ActiveUnit.ResetTurnData();
        ActiveUnit = null;
        isTurnActive = false;

        OnTimelineUpdated?.Invoke(allUnits);
    }

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

    private void HandleCameraRecenterInput()
    {
        if (cameraController != null && ActiveUnit != null)
        {
            cameraController.SetTarget(ActiveUnit.transform, true);
        }
    }
}