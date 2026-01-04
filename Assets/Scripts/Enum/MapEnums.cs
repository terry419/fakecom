using UnityEngine;

// [주인님 코드 유지]
public enum MapSize
{
    [Tooltip("10x10 ~ 15x15")] Small,
    [Tooltip("16x16 ~ 25x25")] Medium,
    [Tooltip("26x26 이상")] Large,
    [Tooltip("특수/초대형")] Huge
}

// [주인님 코드 유지]
public enum MissionType
{
    [Tooltip("적 전멸")] Exterminate,
    [Tooltip("VIP 구출")] Rescue,
    [Tooltip("목표 지점 탈출")] Escape,
    [Tooltip("제한 시간 생존")] Survival,
    [Tooltip("특수 목표")] Special
}

// [Fix] Faction 정의 추가 (누락된 부분)
public enum Faction
{
    Player = 0,
    Enemy = 1,
    Neutral = 2
}

[System.Serializable]
public struct MissionSettings
{
    public string MissionName;
    [TextArea] public string Description;
    public MissionType Type;
    public int TurnLimit;
}