using System;

/// <summary>
/// 바닥 타일의 논리적 속성을 정의하는 데이터 구조.
/// </summary>
[Serializable]
public struct TileLogicData
{
    public FloorType Type;
    public float MoveCost;
    public string FootstepSoundKey;
}

/// <summary>
/// 기둥 타일의 논리적 속성을 정의하는 데이터 구조.
/// </summary>
[Serializable]
public struct PillarLogicData
{
    public PillarType Type;
    public bool IsDestructible;
    public int MaxHp;
}