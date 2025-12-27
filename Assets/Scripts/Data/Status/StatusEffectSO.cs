using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

// 상태이상 분류 (GDD 8.10 기반)
public enum StatusCategory
{
    Injury,     // 부상 (자연 회복 불가, 치료제 필요)
    Debuff,     // 디버프 (시간 지나면 해제)
    Special,    // 특수 (화상, 중독 등)
    System      // 시스템 (차단 주파수 초과 등)
}

// 스탯 보정 정의 (예: 명중률 -30, 이동력 * 0.5)
[System.Serializable]
public struct StatModifierData
{
    public StatType TargetStat; // 수정할 스탯 (Aim, Mobility, Evasion...)
    public float Value;         // 값
    public bool IsMultiplier;   // True면 곱연산(%), False면 합연산(+)
}

[CreateAssetMenu(fileName = "NewStatusEffect", menuName = "Data/Status/StatusEffect")]
public class StatusEffectSO : ScriptableObject
{
    [Header("1. Identity")]
    public string StatusID;         // 식별 ID (예: HeavyBleed)
    public string DisplayName;      // 표시 이름 (예: 과다출혈)
    public Sprite Icon;             // 상태 아이콘
    public StatusCategory Category; // 분류
    [TextArea] public string Description;

    [Header("2. Damage Logic (DOT)")]
    [Tooltip("턴 시작/종료 시 입는 HP 피해량")]
    public int DamagePerTurnHP;

    [Tooltip("턴 시작/종료 시 깎이는 NS(멘탈) 피해량")]
    public int DamagePerTurnNS;

    [Tooltip("이동 1타일당 입는 피해량 (과다출혈용)")]
    public int DamagePerTileMoved;

    [Header("3. Stat Penalties")]
    [Tooltip("이 상태이상이 적용되는 동안 변경될 스탯 목록")]
    public List<StatModifierData> StatModifiers;

    [Header("4. Special Rules")]
    [Tooltip("True일 경우: NS(뉴럴 싱크) 회복 불가 (차단 주파수 초과)")]
    public bool PreventNSRecovery;

    [Tooltip("True일 경우: 방어 QTE 발동 불가 (다리 골절 - 회피 0 고정)")]
    public bool BlockDefenseQTE;

    [Tooltip("True일 경우: 수류탄 등 투척 사거리 반감 (팔 골절)")]
    public bool HalveThrowRange;

    [Header("5. Visuals")]
    public AssetReferenceGameObject EffectVFX; // 캐릭터 몸에 붙을 이펙트
}