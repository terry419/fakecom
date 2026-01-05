using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TurnManager : MonoBehaviour
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

    private void Start()
    {
        InitializeSceneDependencies();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        inputManager = ServiceLocator.Get<InputManager>();
        if (inputManager != null)
        {
            inputManager.OnTurnEndInvoked += EndTurn;
            // [수정] OnCameraRecenter 이벤트를 구독하여 활성 유닛 포커스 기능 수행
            inputManager.OnCameraRecenter += HandleCameraRecenterInput;
        }
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (inputManager != null)
        {
            inputManager.OnTurnEndInvoked -= EndTurn;
            // [수정] 이벤트 구독 해제
            inputManager.OnCameraRecenter -= HandleCameraRecenterInput;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(WaitAndInitialize());
    }
    private IEnumerator WaitAndInitialize()
    {
        yield return null;
        InitializeSceneDependencies();
    }

    private void InitializeSceneDependencies()
    {
        cameraController = ServiceLocator.Get<CameraController>();
        if (inputManager == null) inputManager = ServiceLocator.Get<InputManager>();

        allUnits = FindObjectsOfType<UnitStatus>().OrderBy(u => u.gameObject.name).ToList();

        foreach (var unit in allUnits)
        {
            unit.CurrentTS = Random.Range(20f, 40f) / unit.Agility;
        }

        isTurnActive = false;
        ActiveUnit = null;

        OnTimelineUpdated?.Invoke(allUnits);
    }

    private void Update()
    {
        if (!isTurnActive)
        {
            bool timelineChanged = false;
            float tsToDecrement = tsDecrementPerSecond * Time.deltaTime;

            if (allUnits == null) return;

            foreach (var unit in allUnits)
            {
                if (unit == null) continue;

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
        Debug.Log($"[TurnManager] {unit.name}의 턴 시작. 카메라 이동 시도.");
        isTurnActive = true;
        ActiveUnit = unit;
        ActiveUnit.ResetTurnData();

        Debug.Log($"<color=yellow>[DEBUG_TM] 턴 시작: {unit.name}</color>");
        Debug.Log($"<color=yellow>[DEBUG_TM] 유닛 좌표 확인: {unit.transform.position}</color>");

        if (cameraController != null)
        {
            cameraController.SetTarget(ActiveUnit.transform, false);
        }
        else
        {
            Debug.LogError($"<color=red>[DEBUG_TM] 치명적 오류: CameraController가 Null입니다!</color>");
        }

        ActiveUnit.OnTurnStart();

        // [TODO] 만약 유닛 상태가 Incapacitated(무력화)라면 
        // 50% 확률로 "행동 불능" 로그를 띄우고 바로 EndTurn()을 호출하는 로직 추가 필요
        // 예: if (ActiveUnit.Condition == UnitCondition.Incapacitated && Random.value < 0.5f) { ... }

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
    }

    public void UpdateUnitTurnDelay(UnitStatus unit, float oldTS, float newTS, bool isCrit)
    {
        StartCoroutine(ProcessTurnDelayAnimation(unit, oldTS, newTS, isCrit));
    }

    private IEnumerator ProcessTurnDelayAnimation(UnitStatus unit, float startTS, float targetTS, bool isCrit)
    {
        // ... (이하 생략)
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

    // [추가] CameraRecenter 이벤트 핸들러
    private void HandleCameraRecenterInput()
    {
        if (cameraController != null && ActiveUnit != null)
        {
            cameraController.SetTarget(ActiveUnit.transform, true); // 즉시 이동
        }
    }
}
