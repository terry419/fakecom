using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Data/Weapon/WeaponData")]
public class WeaponDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string WeaponID;
    public string WeaponName;
    public Sprite WeaponIcon;

    [Header("2. Specs")]
    public WeaponType Type;

    [Tooltip("이 무기를 장착할 수 있는 병과")]
    public List<ClassType> AllowedClasses;

    [Tooltip("기본 데미지 (Min ~ Max). 탄약 데미지와 합산될 수 있음.")]
    public MinMaxInt Damage;

    [Tooltip("최대 사거리 (타일 단위)")]
    public int Range;

    [Tooltip("치명타 발생 시 데미지 배율 (기본 1.5)")]
    public float CritBonus = 1.5f;

    [Header("3. Ballistics (Shared Data)")]
    [Tooltip("거리별 명중률/데미지 보정 그래프 (외부 파일 참조)")]
    public CurveDataSO AccuracyCurveData;

    [Header("4. On-Hit Effects (Weapon Intrinsic)")]
    [Tooltip("무기 자체의 특성으로 발동하는 상태이상 (탄약 효과와 별개)")]
    public List<StatusChanceData> OnHitStatusEffects;

    [Header("5. Logic & Visuals")]
    [Tooltip("QTE 미니게임 로직 모듈")]
    public AssetReferenceGameObject ActionModule;

    public AssetReferenceGameObject MuzzleVFX;
    public AssetReferenceGameObject TracerVFX;
    public AssetReferenceGameObject ImpactVFX;
}