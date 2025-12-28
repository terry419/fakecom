using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewTileData", menuName = "Data/Map/TileData")]
public class TileDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public FloorType FloorType;   // 바닥 타입 (Concrete, Grass...)
    public PillarType PillarType; // 기둥 타입 (Concrete, Steel...)

    // 이 데이터가 바닥용인지 기둥용인지 구분
    public bool IsPillarData;

    [Header("2. Visuals")]
    [Tooltip("실제 렌더링될 3D 모델 프리팹")]
    public GameObject ModelPrefab;

    [Header("3. Audio")]
    public string WalkSoundKey; // 밟았을 때 소리
}