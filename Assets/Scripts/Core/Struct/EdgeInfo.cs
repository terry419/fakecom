using System;
using UnityEngine;

[Serializable]
public struct EdgeInfo
{
    public EdgeType Type;
    public CoverType Cover;
    public float CurrentHP;
    public float MaxHP;
    // EdgeDataType 삭제됨

    private EdgeInfo(EdgeType type, CoverType cover, float currentHP, float maxHP)
    {
        Type = type;
        Cover = cover;
        CurrentHP = currentHP;
        MaxHP = maxHP;
    }

    public bool IsDestroyed => MaxHP > 0 && CurrentHP <= 0;

    // 팩토리 메서드 (상수 사용)
    public static EdgeInfo Create(EdgeType type)
    {
        // 1단계: Registry 연동 전 기본값 사용
        float defaultMaxHP = type switch
        {
            EdgeType.Wall => EdgeConstants.HP_WALL,
            EdgeType.Window => EdgeConstants.HP_WINDOW,
            EdgeType.Door => EdgeConstants.HP_DOOR,
            _ => 0
        };

        CoverType defaultCover = type switch
        {
            EdgeType.Wall => EdgeConstants.BASE_COVER_HIGH > 0.3f ? CoverType.High : CoverType.Low,
            EdgeType.Window => CoverType.Low,
            _ => CoverType.None
        };

        return new EdgeInfo(type, defaultCover, defaultMaxHP, defaultMaxHP);
    }

    public static EdgeInfo CreateDamaged(EdgeType type, CoverType cover, float currentHP, float maxHP)
    {
        return new EdgeInfo(type, cover, currentHP, maxHP);
    }

    public EdgeInfo WithDamage(float damage)
    {
        EdgeInfo newInfo = this;
        newInfo.CurrentHP = Mathf.Max(0, this.CurrentHP - damage);
        return newInfo;
    }

    public static EdgeInfo Open => new EdgeInfo(EdgeType.Open, CoverType.None, 0, 0);
}