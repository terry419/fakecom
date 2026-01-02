using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[CreateAssetMenu(fileName = "NewMapPool", menuName = "YCOM/Data/MapPool")]
public class MapPoolSO : ScriptableObject
{
    [Header("Pool Info")]
    public string PoolID;        // 시스템 식별용 (예: "Slum_Easy")
    public string DisplayName;   // UI 표시용 (예: "빈민가 외곽")
    [TextArea] public string Description;

    [field: Header("Settings")]
    [field: SerializeField]
    [field: Range(1, 10)]
    public int TargetDifficulty { get; private set; } = 1;

    [Header("Missions")]
    [Tooltip("이 풀에서 등장 가능한 미션 목록")]
    [SerializeField] private List<MissionDataSO> _missions = new List<MissionDataSO>();

    // 외부에서 리스트에 접근할 때는 읽기 전용으로 제공
    public IReadOnlyList<MissionDataSO> Missions => _missions;

    /// <summary>
    /// 랜덤 미션 추출 (실패 시 false 반환)
    /// </summary>
    public bool TryGetRandomMission(out MissionDataSO mission)
    {
        if (_missions == null || _missions.Count == 0)
        {
            mission = null;
            return false;
        }

        int index = Random.Range(0, _missions.Count);
        mission = _missions[index];
        return true;
    }

    /// <summary>
    /// 데이터 유효성 검사 (기존 로직 계승)
    /// </summary>
    public bool Validate(out string errorMsg)
    {
        if (_missions == null || _missions.Count == 0)
        {
            errorMsg = $"[MapPool {name}] has no missions assigned!";
            return false;
        }

        // 중복 방지 체크 (동일한 MissionDataSO가 두 번 들어갔는지)
        HashSet<MissionDataSO> uniqueCheck = new HashSet<MissionDataSO>();

        for (int i = 0; i < _missions.Count; i++)
        {
            var m = _missions[i];

            // 1. Null 체크
            if (m == null)
            {
                errorMsg = $"[MapPool {name}] Mission at index {i} is NULL!";
                return false;
            }

            // 2. 미션 내부 데이터 검증 (MapData가 있는지 등)
            if (m.MapData == null)
            {
                errorMsg = $"[MapPool {name}] Mission '{m.name}' has NO MapData!";
                return false;
            }

            // 3. 중복 체크
            if (uniqueCheck.Contains(m))
            {
                errorMsg = $"[MapPool {name}] Duplicate Mission found: {m.name}";
                return false;
            }
            uniqueCheck.Add(m);
        }

        errorMsg = string.Empty;
        return true;
    }
}