using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapData", menuName = "Data/Map/MapData")]
public class MapDataSO : ScriptableObject
{
    [Header("1. Map Identity")]
    public string MapID;
    public string DisplayName;

    [Header("2. Environment")]
    [Tooltip("맵의 전체 크기 (실제 데이터가 존재하는 영역의 크기)")]
    public Vector2Int GridSize;

    [Tooltip("맵의 시작 좌표 (오프셋). 예: (10, 10)부터 시작하는 맵")]
    public Vector2Int BasePosition; // [추가됨] 공간 절약을 위한 오프셋 좌표

    [Tooltip("맵의 배경이 될 3D 환경 프리팹 (Visual Only)")]
    public AssetReferenceGameObject MapPrefabRef;

    [Tooltip("층 높이 범위 (예: 0 ~ 5)")]
    public int MinLevel = 0;
    public int MaxLevel = 5;

    [Header("3. Tile Data (Sparse)")]
    [Tooltip("데이터가 존재하는 타일만 저장 (희소 배열)")]
    public List<TileSaveData> Tiles = new List<TileSaveData>();

    [Header("4. Tactical Setup")]
    public List<SpawnPointData> SpawnPoints;

    [Header("5. Enemies & Rewards")]
    [Tooltip("지원군이나 랜덤 인카운터에 사용될 적 풀")]
    public List<EnemyUnitDataSO> EnemyPool;

    [Tooltip("맵 클리어 시 기본 보상")]
    public LootTableSO MapLootTable;
}