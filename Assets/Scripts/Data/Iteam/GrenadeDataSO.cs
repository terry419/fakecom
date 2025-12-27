using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGrenade", menuName = "Data/Item/Grenade")]
public class GrenadeDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string ItemID;
    public string DisplayName;
    public Sprite Icon;

    [Header("2. Throw Specs")]
    [Tooltip("이 수류탄을 사용할 수 있는 병과 (비어있으면 공용)")]
    public List<ClassType> AllowedClasses;

    [Tooltip("투척 사거리 (타일 단위)")]
    public int Range;

    [Tooltip("폭발 범위 반경 (1.5 = 3x3, 2.5 = 5x5)")]
    public float AreaRadius;

    [Tooltip("공격 등급 (T1~T5). 방어구 효율 공식에 적용됨.")]
    public int AttackTier;

    [Header("3. Direct Effect (즉발)")]
    [Tooltip("폭발 즉시 들어가는 피해량 (0이면 데미지 없음)")]
    public float DirectDamage;

    [Tooltip("피해 타입 (Damage:체력, DamageNS:멘탈)")]
    public ItemEffectType DamageType;

    [Tooltip("폭발 적중 시 확률적 상태이상 (예: 기절 30%)")]
    public List<StatusChanceData> DirectStatusEffects;

    [Header("4. Zone Effect (장판)")]
    [Tooltip("폭발 후 생성될 장판 데이터. (Null이면 장판 없음)")]
    public ZoneDataStruct ZoneEffect;

    [Header("5. Tactical")]
    [Tooltip("True일 경우 폭발 범위 내의 전장의 안개(FOW)를 제거하고 은신을 감지함.")]
    public bool IsScan;

    [Header("6. Visuals")]
    public AssetReferenceGameObject ExplosionVFX;
}

[System.Serializable]
public struct ZoneDataStruct
{
    [Tooltip("장판 종류 (ZoneDamage: 화염병 등)")]
    public ItemEffectType ZoneType;

    [Tooltip("장판 지속 턴 수")]
    public int Duration;

    [Tooltip("장판 진입/턴 시작 시 피해량")]
    public float DamagePerTurn;
}