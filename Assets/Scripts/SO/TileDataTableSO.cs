using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 타일(Floor, Pillar)의 '논리적' 데이터를 테이블 형태로 관리하는 ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "TileDataTable", menuName = "SO/Data/TileDataTable")]
public class TileDataTableSO : ScriptableObject
{
    public List<TileLogicData> FloorLogics;
    public List<PillarLogicData> PillarLogics;
}