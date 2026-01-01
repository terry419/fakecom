using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// --- [ 타일 데이터 구조 정의 ] ---

[System.Serializable]
public struct FloorEntry
{
    public FloorType Type;
    public GameObject Prefab;
    public int MoveCost; // 1: 기본, 2: 험지, 999: 이동 불가
}

[System.Serializable]
public struct PillarEntry
{
    public PillarType Type;
    public GameObject Prefab;
    public float MaxHP;

    [Tooltip("지형물의 엄폐 타입 (Standing=높은 엄폐, Broken=낮은 엄폐)")]
    public CoverType Cover;
}

[System.Serializable]
public struct EdgeEntry
{
    public EdgeType Type;
    public GameObject Prefab;
    public float MaxHP;

    [Tooltip("기본 엄폐 수치")]
    public CoverType DefaultCover;

    [Tooltip("체크 시 해당 에지를 넘어갈 수 있음 (예: 창문)")]
    public bool IsPassable;
}

// --- [ 타일 레지스트리 관리 클래스 ] ---

[CreateAssetMenu(fileName = "TileRegistry", menuName = "Data/Map/TileRegistry")]
public class TileRegistrySO : ScriptableObject
{
    [Header("Floors (바닥 타일 목록)")]
    public List<FloorEntry> Floors;

    [Header("Pillars (기둥 및 장애물 목록)")]
    public List<PillarEntry> Pillars;

    [Header("Edges (벽 및 경계선 목록)")]
    public List<EdgeEntry> Edges;

    [Header("Editor Visuals (에디터 시각 설정)")]
    [Tooltip("에디터 마우스 오버 시 하이라이트 재질")]
    public Material HighlightMaterial;
    public Color GridColor = Color.white;
    public Color PillarHighlightColor = Color.yellow;
    public Color EraseHighlightColor = Color.red;

    // --- 내부 캐시 (빠른 조회를 위한 Dictionary) ---
    private Dictionary<FloorType, FloorEntry> _floorCache;
    private Dictionary<PillarType, PillarEntry> _pillarCache;
    private Dictionary<EdgeType, EdgeEntry> _edgeCache;
    private bool _isDirty = true;

    private void OnEnable() => _isDirty = true;

#if UNITY_EDITOR
    private void OnValidate() => _isDirty = true;
#endif

    /// <summary>
    /// 리스트 데이터를 딕셔너리로 변환하여 검색 속도를 최적화합니다.
    /// </summary>
    public void RebuildCache()
    {
        _floorCache = Floors?.ToDictionary(x => x.Type) ?? new Dictionary<FloorType, FloorEntry>();
        _pillarCache = Pillars?.ToDictionary(x => x.Type) ?? new Dictionary<PillarType, PillarEntry>();
        _edgeCache = Edges?.ToDictionary(x => x.Type) ?? new Dictionary<EdgeType, EdgeEntry>();
        _isDirty = false;
    }

    public FloorEntry GetFloor(FloorType type)
    {
        if (_isDirty || _floorCache == null) RebuildCache();
        return _floorCache.GetValueOrDefault(type);
    }

    public PillarEntry GetPillar(PillarType type)
    {
        if (_isDirty || _pillarCache == null) RebuildCache();
        return _pillarCache.GetValueOrDefault(type);
    }

    public EdgeEntry GetEdge(EdgeType type)
    {
        if (_isDirty || _edgeCache == null) RebuildCache();
        return _edgeCache.GetValueOrDefault(type);
    }
}