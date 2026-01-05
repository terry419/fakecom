using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "Game Config/Global Settings")]
public class GlobalSettingsSO : ScriptableObject
{
    // ========================================================================
    // [Section 1] System & Technical Settings (시스템 및 기술 설정)
    // ========================================================================

    [Header("1. App Information")]
    public string gameVersion = "1.0.0";
    public bool isDevelopmentMode = true;

    [Header("2. System Thresholds (시스템 임계값)")]
    [Tooltip("입력 호출 최소값 (데드존)")]
    [Range(0f, 0.5f)] public float inputDeadZone = 0.1f;

    [Tooltip("물리 충돌 최소 수치")]
    public float physicsCollisionThreshold = 5.0f;

    [Tooltip("상호작용 가능 최대 거리")]
    public float interactionDistanceThreshold = 3.0f;

    [Header("3. Graphics & Performance")]
    public int TargetFrameRate = 60;
    public bool useVSync = false;

    [Header("4. Audio Defaults")]
    [Range(0f, 1f)] public float defaultMasterVolume = 1.0f;
    [Range(0f, 1f)] public float defaultBGMVolume = 0.8f;
    [Range(0f, 1f)] public float defaultSFXVolume = 1.0f;


    // ========================================================================
    // [Section 2] Gameplay General (일반 게임플레이)
    // ========================================================================

    [Header("5. Global Gameplay Multipliers")]
    [Tooltip("전체 시간 속도 배율 (1.0 = 기본 속도)")]
    [Range(0f, 5f)] public float globalTimeScale = 1.0f;

    [Tooltip("경험치 획득 배율 (이벤트용 등)")]
    public float experienceMultiplier = 1.0f;

    [Tooltip("전체 데미지 출력 배율")]
    public float damageOutputMultiplier = 1.0f;


    // ========================================================================
    // [Section 3] Neural Sync & Survival System (Plan C 생존 시스템)
    // ========================================================================

    [Header("6. Neural Sync State Thresholds (신경 동기화 상태 임계값)")]
    [Tooltip("Hopeful 상태 임계값 (180 이상)")]
    public float thresholdHopeful = 180f;

    [Tooltip("Inspired 상태 임계값 (150 이상)")]
    public float thresholdInspired = 150f;

    [Tooltip("Normal 상태 임계값 (50 이상)")]
    public float thresholdNormal = 50f;
    // 50 미만 시 Crisis/Error 발생

    [Tooltip("Incapacitated 상태 범위 (35~49)")]
    public float thresholdIncapacitated = 35f;

    [Tooltip("Fleeing 상태 범위 (20~34)")]
    public float thresholdFleeing = 20f;

    [Tooltip("FriendlyFire 상태 범위 (5~19)")]
    public float thresholdFriendlyFire = 5f;


    [Header("7. Survival Multipliers (생존 확률 보정치 M_State)")]
    [Tooltip("Hopeful (180) : x1.5")]
    public float multiplierHopeful = 1.5f;

    [Tooltip("Inspired (150) : x1.2")]
    public float multiplierInspired = 1.2f;

    [Tooltip("Normal (50~149) : x1.0")]
    public float multiplierNormal = 1.0f;

    [Tooltip("Incapacitated (49~35) : x0.8")]
    public float multiplierIncapacitated = 0.8f;

    [Tooltip("Fleeing (34~20) : x0.5")]
    public float multiplierFleeing = 0.5f;

    [Tooltip("FriendlyFire (19~1) : x0.2")]
    public float multiplierFriendlyFire = 0.2f;

    [Tooltip("SelfHarm (0) : x0.0")]
    public float multiplierSelfHarm = 0.0f;

    [Header("8. Overclock Settings (오버클럭 설정)")]
    [Tooltip("싱크 50 미만일 때 매 턴 오버클럭 발동 확률 (5%)")]
    [Range(0f, 1f)] public float overclockChance = 0.05f;

    [Tooltip("오버클럭 성공 시 고정 싱크 수치 (160)")]
    public float overclockSuccessSync = 160f;

    [Header("9. Survival Formula Constants (생존 공식 상수)")]
    [Tooltip("생존 확률 계산 시 최소 싱크 오프셋 (CurrentSync - 5)")]
    public float minSyncOffset = 5f;

    [Tooltip("생존 확률 계산 시 나누는 수 (분모: 175)")]
    public float syncDivisor = 175f;

    [Tooltip("생존 확률 계산 시 기본 배율 (x2)")]
    public float baseFormulaMultiplier = 2.0f;

    [Header("--- NS Crisis Settings ---")]
    [Tooltip("오버클럭 루프가 시작되는 임계값 (기본 50)")]
    public float crisisThreshold = 50f;

    [Tooltip("오버클럭 성공 시 복구될 NS 수치 (기본 160)")]
    public float overclockSuccessValue = 160f;

    [Tooltip("오버클럭 시도 기본 확률 (5%)")]
    [Range(0f, 1f)] public float baseOverclockChance = 0.05f;

    [Tooltip("ClockLock(시스템 잠금) 해제를 위해 도달해야 하는 목표 수치 (기본 100)")]
    public float clockLockReleaseThreshold = 100f;

    [Header("--- QTE Simulation Settings (테스트용) ---")]
    [Tooltip("회피 QTE 성공 확률 (0.0 ~ 1.0)")]
    [Range(0f, 1f)] public float probSurvival = 0.5f;

    [Tooltip("공격 크리티컬 QTE 성공 확률")]
    [Range(0f, 1f)] public float probAttackCrit = 0.7f;

    [Tooltip("적 공격 방어/반격 QTE 성공 확률")]
    [Range(0f, 1f)] public float probEnemyCrit = 0.4f;

    [Tooltip("Synchro-Pulse(오버클럭) QTE 성공 확률")]
    [Range(0f, 1f)] public float probSynchroPulse = 0.3f;

    [Header("--- QTE System Settings ---")]
    [Tooltip("QTE 시작 전, 흐름을 끊지 않기 위한 선딜레이 (초)")]
    public float qtePreDelay = 0.5f;

    [Header("Turn System Penalty Ratios")]
    [Tooltip("일반 피격 시 턴 게이지 페널티 비율 (기본 0.1)")]
    public float tsPenaltyRatioNormal = 0.1f;

    [Tooltip("크리티컬 피격 시 턴 게이지 페널티 비율 (기본 0.2)")]
    public float tsPenaltyRatioCrit = 0.2f;
}