using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewEnemyUnit", menuName = "Data/Unit/EnemyUnit")]
public class EnemyUnitDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string UnitID;
    public string UnitName;

    [Tooltip("적의 등급 (Normal, Elite, Boss)")]
    public EnemyUnitType EnemyType;

    [Tooltip("적의 병과 (Sniper, Assault 등) - AI 행동 패턴 결정")]
    public ClassType Role; // [추가] Role 누락 수정

    [Header("2. Visual")]
    public AssetReferenceGameObject ModelPrefab;
    public AssetReferenceGameObject HitVFX;

    [Header("3. Base Stats")]
    public int MaxHP;
    public int Mobility;
    public int Agility;
    public int Aim;
    public int Evasion;

    [Range(0f, 100f)] // [추가] Player와 동일하게 Range 속성 적용
    public float CritChance = 0f;

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

    [Header("7. Neural Sync")]
    public float BaseSurvivalChance = 0f;
    public float BaseNeuralSync = 100f;
    public float BaseOverclockChance = 0f;
}