using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

public enum ConstraintType
{
    Standard, // 이동 -> 사격 가능 (턴 종료)
    Heavy,    // 이동 -> 사격 불가 (이동 시 사격 비활성)
    Light     // 이동 -> 사격 가능 (턴 유지, 사격 -> 이동 가능)
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Data/Item/Weapon")]
public class WeaponDataSO : ItemDataSO
{
    public override ItemType Type => ItemType.Weapon;

    [Header("2. Specs")]
    public WeaponType WeaponType;

    [Tooltip("이 무기를 장착할 수 있는 병과")]
    public List<ClassType> AllowedClasses;

    [Tooltip("기본 데미지 (Min ~ Max). 탄약 데미지와 합산될 수 있음.")]
    public MinMaxInt Damage;

    [Tooltip("최대 사거리 (타일 단위)")]
    public int Range;

    [Tooltip("치명타 발생 시 데미지 배율 (기본 1.5)")]
    public float CritBonus = 1.5f;

    [Tooltip("명중 시 대상의 TS(Time Score) 페널티 (저지력). 높을수록 적의 턴이 늦게 옴.")]
    public float HitImpactPenalty = 10f;

    // [New] 기본 탄약 데이터 추가 (필수)
    // 인벤토리 시스템이 없거나, 적 유닛일 경우 이 탄약을 기본으로 사용함.
    [Header("2.1 Default Ammo")]
    [Tooltip("장착된 탄약이 없을 때 사용할 기본 탄약 (AttackTier 참조용)")]
    public AmmoDataSO DefaultAmmo;

    [Header("3. Tactical Logic")]

    [Tooltip("행동 제약 유형 (Standard: 일반 / Heavy: 이동후사격불가 / Light: 사격후이동가능)")]
    public ConstraintType Constraint = ConstraintType.Standard;

    [Header("4. Durability")]
    [Tooltip("최대 내구도 (0이 되면 파손/수리 필요)")]
    public float MaxDurability = 100f;

    [Header("5. Ballistics (Shared Data)")]
    [Tooltip("거리별 명중률/데미지 보정 그래프 (외부 파일 참조)")]
    public CurveDataSO AccuracyCurveData;

    [Header("6. On-Hit Effects (Weapon Intrinsic)")]
    [Tooltip("무기 자체의 특성으로 발동하는 상태이상 (탄약 효과와 별개)")]
    public List<StatusChanceData> OnHitStatusEffects;

    [Header("7. Logic & Visuals")]
    [Tooltip("QTE 미니게임 로직 모듈")]
    public AssetReferenceGameObject ActionModule;

    [Tooltip("발사될 투사체 프리팹 (Projectile 컴포넌트 포함)")]
    public AssetReferenceGameObject ProjectilePrefab;

    [Header("Visuals (VFX Only)")]
    public AssetReferenceGameObject MuzzleVFX;
    public AssetReferenceGameObject TracerVFX;
    public AssetReferenceGameObject ImpactVFX;

    private void OnEnable()
    {
        MaxStack = 1; // 장비는 스택 불가
    }
}