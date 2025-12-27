using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewEnemyUnit", menuName = "Data/Unit/EnemyUnit")]
public class EnemyUnitDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string UnitID;
    public string UnitName;
    public EnemyUnitType EnemyType; // Normal, Elite, Boss

    [Header("2. Visual")]
    public AssetReferenceGameObject ModelPrefab;
    public AssetReferenceGameObject HitVFX; // 피격 시 발생하는 유닛 고유 이펙트

    [Header("3. Base Stats")]
    public int MaxHP;
    public int Mobility;
    public int Agility;
    public int Aim;
    public int Evasion;

    [Header("4. AI Intelligence (Learning Data)")]
    [Tooltip("유닛 본인의 지능 수준 (1~10). 딥러닝 스코어 가중치에 영향을 줍니다.")]
    public int BaseAILevel = 1;

    [Tooltip("주변 아군에게 부여하는 지능 보너스/페널티. 음수도 가능합니다.")]
    public int CommandAIBonus = 0;

    [Header("5. Enemy Specific")]
    public LootTableSO DropTable;
}