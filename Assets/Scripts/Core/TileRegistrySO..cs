using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// [구조체 외부 선언]

[System.Serializable]
public struct FloorEntry
{
    public FloorType Type;
    public GameObject Prefab;
    public int MoveCost; // 1: 기본, 2: 험지, 999: 불가
}

[System.Serializable]
public struct PillarEntry
{
    public PillarType Type;
    public GameObject Prefab;
    public float MaxHP;

    [Tooltip("기둥이 제공하는 엄폐 등급 (Standing=High, Broken=Low)")]
    public CoverType Cover;
}

[System.Serializable]
public struct EdgeEntry
{
    public EdgeType Type;
    public GameObject Prefab;
    public float MaxHP;

    [Tooltip("기본 엄폐 등급")]
    public CoverType DefaultCover;

    [Tooltip("체크 시 유닛이 넘어갈 수 있음 (예: 창문)")]
    public bool IsPassable;
}

[CreateAssetMenu(fileName = "TileRegistry", menuName = "Data/Map/TileRegistry")]
public class TileRegistrySO : ScriptableObject
{
    [Header("Floors")]
    public List<FloorEntry> Floors;

    [Header("Pillars")]
    public List<PillarEntry> Pillars;

    [Header("Edges")]
    public List<EdgeEntry> Edges;

    // --- 런타임 캐싱 (Dictionary) ---
    private Dictionary<FloorType, FloorEntry> _floorCache;
    private Dictionary<PillarType, PillarEntry> _pillarCache;
    private Dictionary<EdgeType, EdgeEntry> _edgeCache;
    private bool _isDirty = true;

    private void OnEnable() => _isDirty = true;
#if UNITY_EDITOR
    private void OnValidate() => _isDirty = true;
#endif

    private void RebuildCache()
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