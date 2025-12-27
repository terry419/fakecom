using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewAmmo", menuName = "Data/Item/Ammo")]
public class AmmoDataSO : ItemDataSO // [변경] 상속 변경
{
    public override ItemType Type => ItemType.Ammo;

    [Header("2. Combat Specs")]
    [Tooltip("공격 등급 (T1~T5). 방어구 등급과 비교하여 데미지 효율을 결정합니다.")]
    [Range(0, 5)]
    public int AttackTier;

    [Header("3. Restrictions")]
    [Tooltip("이 탄약을 사용할 수 있는 무기 타입 (비어있으면 모든 무기 사용 가능)")]
    public List<WeaponType> AllowedWeaponTypes;
    // 예: Sniper 전용 철갑탄 -> List에 Sniper만 추가.

    [Header("4. On-Hit Effects")]
    [Tooltip("적중 시 발동할 확률적 상태이상 목록 (예: 10% 출혈, 5% 골절)")]
    public List<StatusChanceData> StatusEffects;

    [Header("5. Visuals")]
    [Tooltip("이 탄약 사용 시 덮어씌울 탄착/발사 이펙트 (Null이면 무기 기본값 사용)")]
    public AssetReferenceGameObject VFX_Override;

}