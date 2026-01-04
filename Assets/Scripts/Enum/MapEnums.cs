using UnityEngine;

// [기존 유지]
public enum MapSize
{
    [Tooltip("10x10 ~ 15x15")] Small,
    [Tooltip("16x16 ~ 25x25")] Medium,
    [Tooltip("26x26 이상")] Large,
    [Tooltip("초거대/무한")] Huge
}

// [기존 유지]
public enum MissionType
{
    [Tooltip("전멸")] Exterminate,
    [Tooltip("VIP 구출")] Rescue,
    [Tooltip("목표 지점 탈출")] Escape,
    [Tooltip("턴 버티기")] Survival,
    [Tooltip("특수 목표")] Special
}

// [기존 유지]
public enum Faction
{
    Player = 0,
    Enemy = 1,
    Neutral = 2
}


// [Refactor] MissionSettings -> MissionDefinition (네이밍 충돌 방지 및 의미 명확화)
[System.Serializable]
public struct MissionDefinition
{
    public string MissionName;
    [TextArea] public string Description;
    public MissionType Type;
    public int TimeLimit; // 0 = 무제한
}