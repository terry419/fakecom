using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewEnemyUnit", menuName = "Data/Unit/EnemyUnit")]
public class EnemyUnitDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string UnitID;
    public string UnitName;
    public EnemyUnitType EnemyType; // 일반, 엘리트, 보스

    [Header("2. Visual")]
    public AssetReferenceGameObject ModelPrefab;
    public AssetReferenceGameObject HitVFX; // 피격 시 발생하는 타격 이펙트 에셋

    [Header("3. Base Stats")]
    public int MaxHP;
    public int Mobility;
    public int Agility;
    public int Aim;
    public int Evasion;
    public float CritChance = 0f;

    [Header("4. AI Intelligence (Learning Data)")]
    [Tooltip("기본 인공지능 레벨 (1~10). 레벨이 높을수록 전술적 판단 수치가 상승합니다.")]
    public int BaseAILevel = 1;

    [Tooltip("주변 아군에게 부여하는 지휘 보너스/페널티 수치입니다.")]
    public int CommandAIBonus = 0;

    [Header("5. Enemy Specific")]
    public LootTableSO DropTable;

    [Header("6. Neural Sync")]
    [Tooltip("기본 생존 확률 (기본값 0)")]
    public float BaseSurvivalChance = 0f;
    [Tooltip("초기 싱크로율 (기본 100)")]
    public float BaseNeuralSync = 100f;
    [Tooltip("오버클럭 성공 기본 확률")]
    public float BaseOverclockChance = 0f;
}