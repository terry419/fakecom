// 경로: Assets/Scripts/Core/Structs/EdgeInfo.cs
using System;
using UnityEngine;

[Serializable]
public struct EdgeInfo
{
    public EdgeType Type;
    public CoverType Cover;
    public float CurrentHP;
    public float MaxHP;
    public EdgeDataType DataType;

    private EdgeInfo(EdgeType type, CoverType cover, float currentHP, float maxHP, EdgeDataType dataType)
    {
        Type = type;
        Cover = cover;
        CurrentHP = currentHP;
        MaxHP = maxHP;
        DataType = dataType;
    }

    public bool IsDestroyed => MaxHP > 0 && CurrentHP <= 0;

    /// <summary>
    /// [Factory] 엣지 생성. EdgeDataSO가 있으면 그 값을 쓰고, 없으면 기본값을 씁니다.
    /// </summary>
    public static EdgeInfo Create(EdgeType type, EdgeDataType dataType, EdgeDataSO edgeData = null)
    {
        // 1. 데이터(SO)가 있으면 그걸 우선 사용 (Data-Driven)
        if (edgeData != null)
        {
            return new EdgeInfo(type, edgeData.DefaultCover, edgeData.MaxHP, edgeData.MaxHP, dataType);
        }

        // 2. 데이터가 없으면 하드코딩 기본값 (Fallback)
        float defaultMaxHP = type switch
        {
            EdgeType.Wall => 100,
            EdgeType.Window => 30,
            EdgeType.Door => 50,
            _ => 0
        };

        return new EdgeInfo(type, CoverType.None, defaultMaxHP, defaultMaxHP, dataType);
    }

    public static EdgeInfo CreateDamaged(EdgeType type, CoverType cover, float currentHP, float maxHP, EdgeDataType dataType)
    {
        return new EdgeInfo(type, cover, currentHP, maxHP, dataType);
    }

    public EdgeInfo WithDamage(float damage)
    {
        EdgeInfo newInfo = this;
        newInfo.CurrentHP = Mathf.Max(0, this.CurrentHP - damage);
        return newInfo;
    }

    public static EdgeInfo Open => Create(EdgeType.Open, EdgeDataType.None, null);
}