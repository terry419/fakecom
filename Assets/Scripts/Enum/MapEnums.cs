using UnityEngine;

public enum MapSize
{
    [Tooltip("10x10 ~ 15x15")] Small,
    [Tooltip("16x16 ~ 25x25")] Medium,
    [Tooltip("26x26 이상")] Large,
    [Tooltip("특수/초대형")] Huge
}

public enum MissionType
{
    [Tooltip("적 전멸")] Exterminate,
    [Tooltip("VIP 구출")] Rescue,
    [Tooltip("목표 지점 탈출")] Escape,
    [Tooltip("제한 시간 생존")] Survival,
    [Tooltip("특수 목표")] Special
}