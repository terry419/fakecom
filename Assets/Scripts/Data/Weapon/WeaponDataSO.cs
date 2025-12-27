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
    public List<ClassType> AllowedClasses;
    public MinMaxInt Damage;
    public int Range;
    public float CritBonus = 1.5f;

    [Header("3. Ballistics")]
    public AnimationCurve AccuracyCurve;
    // DmgFalloffCurve는 사용자 요청으로 삭제되었습니다.

    [Header("4. Visual Effects (Hitscan)")]
    public AssetReferenceGameObject MuzzleVFX;  // 발사 효과
    public AssetReferenceGameObject TracerVFX;  // 궤적 효과
    public AssetReferenceGameObject ImpactVFX;  // 피격 위치 폭발/탄착 효과
}