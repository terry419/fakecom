using UnityEngine;

// [Refactoring Phase 3] 두 타일이 공유하는 실제 벽 객체 (SSOT 준수)
// struct가 아닌 class여야 참조 공유가 가능합니다.
public class RuntimeEdge
{
    public EdgeType Type { get; private set; }
    public CoverType Cover { get; private set; }
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }

    // 파괴 여부: HP 시스템이 있는 벽인데, 체력이 다했다면 파괴됨
    public bool IsBroken => MaxHP > 0 && CurrentHP <= 0;

    // [이동 판정] 물리적으로 길을 막고 있는가?
    // - 벽이나 문, 펜스이면서
    // - 아직 부서지지 않았을 때만 True
    // - (참고: Window는 이동 가능(Passable)으로 간주하거나 넘기 모션 필요)
    public bool IsBlocking
    {
        get
        {
            if (Type == EdgeType.Open) return false;
            if (IsBroken) return false;
            if (Type == EdgeType.Window) return false; // 창문은 이동 가능(넘어가기)

            return true; // Wall, Door, Fence 등
        }
    }

    // [사격 판정] 투사체가 통과하는가?
    // - 뚫려있거나(Open), 창문이거나, 부서졌으면 통과
    public bool IsPermeable => Type == EdgeType.Open || Type == EdgeType.Window || IsBroken;

    public RuntimeEdge(EdgeType type, CoverType cover, float maxHP, float currentHP)
    {
        Type = type;
        Cover = cover;
        MaxHP = maxHP;
        CurrentHP = currentHP;
    }

    public void TakeDamage(float amount)
    {
        if (CurrentHP <= 0) return;

        CurrentHP -= amount;
        if (CurrentHP < 0) CurrentHP = 0;
    }
}