using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewEnemyUnit", menuName = "Data/Unit/EnemyUnit")]
public class EnemyUnitDataSO : UnitDataSO
{

    [Tooltip("적의 등급 (Normal, Elite, Boss)")]
    public EnemyUnitType EnemyType;


    [Header("4. Loadout")] // [추가] 장비 슬롯 신설
    [Tooltip("적 주무기 (사거리/데미지 결정)")]
    public WeaponDataSO MainWeapon;

    [Tooltip("적 방어구 (방어 등급 결정)")]
    public ArmorDataSO BodyArmor;

    [Header("5. AI Intelligence")]
    [Tooltip("AI 레벨 (1~10). 높을수록 정교한 전술 구사.")]
    public int BaseAILevel = 1;

    [Tooltip("주변 아군에게 부여하는 지휘 보너스.")]
    public int CommandAIBonus = 0;

    [Header("6. Drops")]
    public LootTableSO DropTable;

}