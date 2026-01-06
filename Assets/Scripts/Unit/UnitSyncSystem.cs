using UnityEngine;
using System.Threading.Tasks;

// 이 컴포넌트는 UnitStatus와 함께 존재해야 합니다.
[RequireComponent(typeof(UnitStatus))]
public class UnitSyncSystem : MonoBehaviour
{
    [Header("--- Settings ---")]
    [SerializeField] private GlobalSettingsSO globalSettings;

    [Header("--- Neural Sync Data ---")]
    [Range(0, 200)]
    [SerializeField] private float currentSync; // 신경 동기화 수치 (0~200)

    // 상태 플래그
    private bool hasSynchroPulseTriggered = false; // 싱크로 펄스 1회 발동 체크
    public bool IsClockLocked { get; private set; } = false; // 시스템 과부하(클락 락) 상태 여부

    // 외부 참조
    private UnitStatus _unitStatus;

    public float CurrentSync => currentSync;

    public void Initialize(UnitStatus owner, GlobalSettingsSO settings)
    {
        _unitStatus = owner;
        globalSettings = settings;

        // 초기화 로직 (UnitData에 수치가 없다면 기본 100)
        currentSync = 100f;
        IsClockLocked = false;
        hasSynchroPulseTriggered = false;
    }

    /// <summary>
    /// 신경 동기화 수치를 갱신하고 상태 변화를 체크합니다.
    /// </summary>
    public void UpdateSync(float amount, string reason = "")
    {
        if (globalSettings == null) return;

        // 1. 변화 전 수치 기록
        float previousSync = currentSync;

        // 2. 수치 합산 및 클램핑 (0~200)
        currentSync = Mathf.Clamp(currentSync + amount, 0f, 200f);

        // 3. [ClockLock 해제 검사]
        if (IsClockLocked && amount > 0)
        {
            if (currentSync >= 100f)
            {
                IsClockLocked = false;
                Debug.Log($"<color=green>[System Restored] {name}의 ClockLock이 해제되었습니다!</color>");
            }
        }

        // 4. [Synchro-Pulse 발동 검사]
        // 정상 범주(50 이상)에서 미만으로 떨어졌을 때 1회에 한해 펄스 발생
        bool isDroppingBelowThreshold = (previousSync >= globalSettings.thresholdNormal && currentSync < globalSettings.thresholdNormal);

        if (!IsClockLocked && isDroppingBelowThreshold && !hasSynchroPulseTriggered)
        {
            HandleSynchroPulse();
        }

        // 5. 상태 갱신 (UnitStatus의 Condition 변경)
        UpdateConditionFromSync();
    }

    /// <summary>
    /// 싱크로 수치가 급격히 낮아질 때 발생하는 최후의 보루(펄스)를 처리합니다.
    /// </summary>
    private void HandleSynchroPulse()
    {
        if (hasSynchroPulseTriggered) return;

        hasSynchroPulseTriggered = true; // 일회성 발동 체크

        // 1. 오버클럭 시도 확률 체크
        if (Random.value <= globalSettings.baseOverclockChance)
        {
            Debug.Log($"<color=cyan>[Synchro-Pulse] {name} 임계 동기화 도달! QTE 발생...</color>");

            // 2. QTE 매니저 호출
            var qte = ServiceLocator.Get<QTEManager>();
            if (qte != null)
            {
                qte.StartQTE(QTEType.SynchroPulse, (bool isSuccess) =>
                {
                    // 3. QTE 결과 반영
                    if (isSuccess)
                    {
                        // [성공] 오버클럭 발동
                        currentSync = globalSettings.overclockSuccessValue; // 약 160
                        IsClockLocked = false;
                        UpdateConditionFromSync();
                        Debug.Log($"<color=cyan>[Overclock] {name} 시스템 한계 돌파!</color>");
                    }
                    else
                    {
                        // [실패] 시스템 잠금
                        IsClockLocked = true;
                        UpdateConditionFromSync();
                        Debug.Log("<color=red>[ClockLock] QTE 실패... 시스템이 잠깁니다.</color>");
                    }
                });
            }
            else
            {
                // 매니저가 없을 경우 기본적으로 잠금 처리
                IsClockLocked = true;
                UpdateConditionFromSync();
            }
        }
        else
        {
            // 확률을 뚫지 못했을 경우 바로 잠금
            IsClockLocked = true;
            UpdateConditionFromSync();
            Debug.Log("<color=red>[ClockLock] 오버클럭 기회를 놓쳤습니다. (확률 실패)</color>");
        }
    }

    /// <summary>
    /// 현재 신경 동기화 수치에 기반하여 유닛의 상태(Condition)를 결정합니다.
    /// </summary>
    private void UpdateConditionFromSync()
    {
        if (_unitStatus == null) return;

        // 1. [ClockLock 우선 체크]
        if (IsClockLocked)
        {
            _unitStatus.Condition = UnitCondition.Incapacitated;
            return;
        }

        // 2. [수치 기반 상태 결정]
        UnitCondition newCondition;

        if (currentSync >= globalSettings.thresholdHopeful) newCondition = UnitCondition.Hopeful;
        else if (currentSync >= globalSettings.thresholdInspired) newCondition = UnitCondition.Inspired;
        else if (currentSync >= globalSettings.thresholdNormal) newCondition = UnitCondition.Normal;
        else
        {
            // 50 미만 하향 구간
            if (currentSync >= globalSettings.thresholdIncapacitated) newCondition = UnitCondition.Incapacitated;
            else if (currentSync >= globalSettings.thresholdFleeing) newCondition = UnitCondition.Fleeing;
            else if (currentSync >= globalSettings.thresholdFriendlyFire) newCondition = UnitCondition.FriendlyFire;
            else newCondition = UnitCondition.SelfHarm;
        }

        // 상태가 다를 때만 UnitStatus에 적용
        if (_unitStatus.Condition != newCondition)
        {
            _unitStatus.Condition = newCondition;
        }
    }
}