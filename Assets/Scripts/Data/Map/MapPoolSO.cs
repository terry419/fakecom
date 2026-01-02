using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapPool", menuName = "Data/Map/MapPool")]
public class MapPoolSO : ScriptableObject
{
    [field: Header("Pool Settings")]
    [field: SerializeField]
    [field: Range(1, 10)]
    public int TargetDifficulty { get; private set; } = 1;

    [field: Header("Entries")]
    [field: SerializeField]
    public List<MapEntry> Entries { get; private set; } = new List<MapEntry>();

    public bool TryGetRandomEntry(out MapEntry entry)
    {
        if (Entries == null || Entries.Count == 0)
        {
            entry = default;
            return false;
        }

        int index = UnityEngine.Random.Range(0, Entries.Count);
        entry = Entries[index];
        return true;
    }

    public bool Validate(out string errorMsg)
    {
        if (Entries == null || Entries.Count == 0)
        {
            errorMsg = $"[Pool {name}] has no entries!";
            return false;
        }

        // [개선 4] Fail-Fast: 첫 에러에서 즉시 반환
        HashSet<string> localIds = new HashSet<string>();
        for (int i = 0; i < Entries.Count; i++)
        {
            var e = Entries[i];

            if (!e.Validate(out string entryErr))
            {
                errorMsg = $"[Pool {name}] Entry {i} Error: {entryErr}";
                return false;
            }

            if (!string.IsNullOrEmpty(e.MapID))
            {
                if (localIds.Contains(e.MapID))
                {
                    errorMsg = $"[Pool {name}] Duplicate MapID inside pool: {e.MapID}";
                    return false;
                }
                localIds.Add(e.MapID);
            }
        }

        errorMsg = string.Empty;
        return true;
    }
}