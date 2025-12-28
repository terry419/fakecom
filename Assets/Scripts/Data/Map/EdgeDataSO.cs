// 경로: Assets/Scripts/Data/Map/EdgeDataSO.cs
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewEdgeData", menuName = "Data/Map/EdgeData")]
public class EdgeDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public EdgeDataType DataType;

    [Header("2. Stats")]
    [Tooltip("이 벽의 최대 체력 (예: 콘크리트=150, 철강=200)")]
    public float MaxHP = 100f;

    [Tooltip("기본 엄폐도 (None=0%, Low=20%, High=40%)")]
    public CoverType DefaultCover = CoverType.High;

    [Header("3. Visuals")]
    [Tooltip("벽이나 창문의 3D 모델 프리팹")]
    public GameObject ModelPrefab;

    [Tooltip("파괴되었을 때 교체될 잔해 프리팹 (Optional)")]
    public GameObject DebrisPrefab;

    [Tooltip("총알이 박혔을 때의 이펙트 타입 (Concrete, Metal, Wood...)")]
    public string MaterialType;

    [Header("4. Audio")]
    public string HitSoundKey;      // 피격음
    public string DestroySoundKey;  // 파괴음
}