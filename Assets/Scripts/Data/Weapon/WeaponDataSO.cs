using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

// 이동 후 사격 제약 (Standard: 가능, Heavy: 이동 후 불가, Light: 사격 후 이동 가능)
public enum WeaponConstraint { Standard, Heavy, Light }

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

    [Header("3. Tactical Logic")]
    [Tooltip("행동 제약 유형 (이동-사격 관계)")]
    public WeaponConstraint ConstraintType;

    [Tooltip("True: 사격 시 턴 강제 종료 / False: 권총 등 (AP 남으면 행동 가능)")]
    public bool EndsTurn = true;

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

    public AssetReferenceGameObject MuzzleVFX;
    public AssetReferenceGameObject TracerVFX;
    public AssetReferenceGameObject ImpactVFX;

    private void OnEnable()
    {
        MaxStack = 1; // 장비는 스택 불가
    }

}