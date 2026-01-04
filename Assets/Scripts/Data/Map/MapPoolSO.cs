using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewMapPool", menuName = "Data/Map/MapPool")]
public class MapPoolSO : ScriptableObject
{
    [field: Header("Pool Settings")]
    [field: SerializeField]
    [field: Range(1, 10)]
    public int TargetDifficulty { get; private set; } = 1;

    [Header("Missions")]
    // [Fix] MapEntry(단순 정보) 대신 MissionDataSO(완성 미션)를 직접 관리
    public List<MissionDataSO> Missions = new List<MissionDataSO>();

    // [Fix] 랜덤한 '미션'을 반환하도록 수정
    public bool TryGetRandomMission(out MissionDataSO mission)
    {
        if (Missions == null || Missions.Count == 0)
        {
            mission = null;
            return false;
        }

        int index = UnityEngine.Random.Range(0, Missions.Count);
        mission = Missions[index];
        return mission != null;
    }

    public bool Validate(out string errorMsg)
    {
        if (Missions == null || Missions.Count == 0)
        {
            errorMsg = $"[Pool {name}] has no missions!";
            return false;
        }

        // 미션 데이터 내부 검증 (MapRef가 있는지 등)
        for (int i = 0; i < Missions.Count; i++)
        {
            if (Missions[i] == null)
            {
                errorMsg = $"[Pool {name}] Mission at index {i} is null!";
                return false;
            }
            if (Missions[i].MapDataRef == null || !Missions[i].MapDataRef.RuntimeKeyIsValid())
            {
                errorMsg = $"[Pool {name}] Mission '{Missions[i].name}' has invalid MapDataRef!";
                return false;
            }
        }

        errorMsg = string.Empty;
        return true;
    }
}