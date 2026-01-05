using UnityEngine;
using System;

/// <summary>
/// 게임 저장 시 유닛의 '변하는 상태(체력, 위치, 경험치)'를 기록하는 데이터 클래스입니다.
/// JSON 등으로 직렬화되기 위해 [Serializable]이 필요합니다.
/// </summary>
[Serializable]
public class UnitSaveData
{
    [Header("Identity")]
    public string unitID;      // UnitDataSO를 찾기 위한 고유 키 (파일명 등)
    public int faction;        // 0: Player, 1: Enemy, 2: Neutral

    [Header("Variable Stats")]
    public int CurrentHP;      // UnitStatus에서 참조 중인 핵심 필드
    public int Experience;     // (확장용) 경험치

    [Header("Position")]
    public Vector3Int GridPos; // 저장된 그리드 좌표

    // 기본 생성자 (직렬화용)
    public UnitSaveData() { }

    // 초기 생성 헬퍼
    public UnitSaveData(string id, int hp, Vector3Int pos, int factionIndex = 0)
    {
        unitID = id;
        CurrentHP = hp;
        GridPos = pos;
        faction = factionIndex;
        Experience = 0;
    }
}