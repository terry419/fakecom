using UnityEngine;
using System.Collections.Generic;

// --- 매핑용 구조체 ---
// Floor
[System.Serializable]
public struct EditorFloorMapping
{
    public FloorType type;
    public GameObject prefab;
    public Material editorMaterial; // 에디터 씬뷰 시각화용 (선택사항)
}

// Pillar
[System.Serializable]
public struct EditorPillarMapping
{
    public PillarType type;
    public GameObject prefab;
    public Material editorMaterial;
}

// Edge (Wall/Window/Door)
[System.Serializable]
public struct EditorEdgeMapping
{
    public EdgeType type;
    public GameObject prefab;
    public Material editorMaterial;
}

[CreateAssetMenu(fileName = "MapEditorSettings", menuName = "Editor/Map Editor Settings")]
public class MapEditorSettingsSO : ScriptableObject
{
    [Header("Default Prefabs")]
    public GameObject DefaultTilePrefab;
    public GameObject DefaultPillarPrefab;
    public GameObject DefaultWallPrefab; // 기본 벽 프리팹 (EdgeType.Wall)
    public GameObject DefaultWindowPrefab; // 기본 창문 프리팹 (EdgeType.Window)
    public GameObject DefaultDoorPrefab;   // 기본 문 프리팹 (EdgeType.Door)


    [Header("Mappings")]
    public List<EditorFloorMapping> FloorMappings = new List<EditorFloorMapping>();
    public List<EditorPillarMapping> PillarMappings = new List<EditorPillarMapping>();
    public List<EditorEdgeMapping> EdgeMappings = new List<EditorEdgeMapping>();

    [Header("Editor Visuals")]
    public Material TileHighlightMaterial; // 선택된 타일 하이라이트용
    public Material GridHighlightMaterial; // 그리드 스냅 포지션 표시용
    public Material EdgeHighlightMaterial; // 마우스 오버 엣지 하이라이트용
    public Material ErrorMaterial; // 오류 표시용 (예: 맵 범위 밖)

    [Header("Editor Settings")]
    public float MapCellSize = 1.0f; // GridUtils.CELL_SIZE와 동일하게
    public float MapLevelHeight = 2.5f; // GridUtils.LEVEL_HEIGHT와 동일하게
}
