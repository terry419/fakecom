using UnityEngine;
using UnityEngine.AddressableAssets; // Addressables 패키지 필수
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Data/Map/MapData")]
public class MapDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string MapID;
    public string DisplayName;

    [Header("2. Environment")]
    // 맵의 최대 크기 (카메라 이동 제한 및 A* 경계용)
    public Vector2Int GridSize;
    // 실제 맵 프리팹 (Addressable로 비동기 로드)
    public AssetReferenceGameObject MapPrefabRef;

    [Header("3. Tactical Setup")]
    // 아군 배치 구역 및 적군 초기 위치
    public List<SpawnPointData> SpawnPoints;

    [Header("4. Enemies & Rewards")]
    // 이 맵에서 증원(Reinforce) 등으로 추가 등장 가능한 적 유닛 풀
    public List<EnemyUnitDataSO> EnemyPool;
    // 맵 클리어 또는 상자 파밍 시 획득할 보상 테이블
    public LootTableSO MapLootTable;
}