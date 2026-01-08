using UnityEngine;
using System.Collections.Generic;
using System.Linq;

// ==================================================================================
// 1. 데이터 구조체 정의
// ==================================================================================

[System.Serializable]
public struct FloorEntry
{
    public FloorType Type;
    public GameObject Prefab;
    public int MoveCost;
}

[System.Serializable]
public struct PillarEntry
{
    public PillarType Type;
    public GameObject Prefab;
    public float MaxHP;
    public CoverType Cover;
}

[System.Serializable]
public struct EdgeEntry
{
    public EdgeType Type;
    public GameObject Prefab;
    public float MaxHP;
    public CoverType DefaultCover;
    public bool IsPassable;
}

// [New] 스폰 프리팹 구조체 (누락되었던 부분 추가)
[System.Serializable]
public struct SpawnEntry
{
    public MarkerType Type; // PlayerSpawn or EnemySpawn
    public GameObject Prefab;
}

public enum PortalType
{
    In, Out, Both
}

[System.Serializable]
public struct PortalEntry
{
    public PortalType Type;
    public GameObject Prefab;
}

// ==================================================================================
// 2. 레지스트리 메인 클래스
// ==================================================================================

[CreateAssetMenu(fileName = "TileRegistry", menuName = "Data/Map/TileRegistry")]
public class TileRegistrySO : ScriptableObject
{
    [Header("Floors")] public List<FloorEntry> Floors;
    [Header("Pillars")] public List<PillarEntry> Pillars;
    [Header("Edges")] public List<EdgeEntry> Edges;
    [Header("Portals")] public List<PortalEntry> Portals;

    // [Fix] 스폰 리스트 추가
    [Header("Spawns")] public List<SpawnEntry> Spawns;

    [Header("Editor Visuals")]
    public Material HighlightMaterial;
    public Color GridColor = Color.white;
    public Color PillarHighlightColor = Color.yellow;
    public Color EraseHighlightColor = Color.red;

    [Header("Marker Colors")]
    public Color PlayerSpawnColor = Color.green;
    public Color EnemySpawnColor = Color.red;
    public Color PortalInColor = new Color(0.5f, 0, 1f);
    public Color PortalOutColor = new Color(0, 0.5f, 1f);

    // --- 캐시 ---
    private Dictionary<FloorType, FloorEntry> _floorCache;
    private Dictionary<PillarType, PillarEntry> _pillarCache;
    private Dictionary<EdgeType, EdgeEntry> _edgeCache;
    private bool _isDirty = true;
    private void OnEnable() => _isDirty = true;
#if UNITY_EDITOR
    private void OnValidate() => _isDirty = true;
#endif
    public void RebuildCache()
    {
        _floorCache = Floors?.ToDictionary(x => x.Type) ?? new Dictionary<FloorType, FloorEntry>();
        _pillarCache = Pillars?.ToDictionary(x => x.Type) ?? new Dictionary<PillarType, PillarEntry>();
        _edgeCache = Edges?.ToDictionary(x => x.Type) ?? new Dictionary<EdgeType, EdgeEntry>();
        _isDirty = false;
    }
    public FloorEntry GetFloor(FloorType type) { if (_isDirty || _floorCache == null) RebuildCache(); return _floorCache.GetValueOrDefault(type); }
    public PillarEntry GetPillar(PillarType type) { if (_isDirty || _pillarCache == null) RebuildCache(); return _pillarCache.GetValueOrDefault(type); }
    public EdgeEntry GetEdge(EdgeType type) { if (_isDirty || _edgeCache == null) RebuildCache(); return _edgeCache.GetValueOrDefault(type); }
    public PortalEntry GetPortalPrefab(PortalType type)
    {
        if (Portals == null || Portals.Count == 0) return default;
        var entry = Portals.FirstOrDefault(p => p.Type == type);
        return (entry.Prefab != null) ? entry : Portals[0];
    }

    // [Fix] 스폰 프리팹 조회 메서드 구현 (Action 스크립트에서 호출함)
    public GameObject GetSpawnPrefab(MarkerType type)
    {
        if (Spawns == null || Spawns.Count == 0) return null;
        var entry = Spawns.FirstOrDefault(s => s.Type == type);
        return entry.Prefab;
    }
}