using UnityEngine;

// [Refactoring Phase 3] 두 타일이 공유하는 실제 벽 객체 (SSOT 준수)
public class RuntimeEdge
{
    public EdgeType Type { get; private set; }
    public CoverType Cover { get; private set; }
    public float MaxHP { get; private set; }
    public float CurrentHP { get; private set; }

    // [New] 데이터(TileRegistry)에서 가져온 기본 통과 가능 여부
    private bool _basePassable;

    // 파괴 여부: HP 시스템이 있는 벽인데, 체력이 다했다면 파괴됨
    public bool IsBroken => MaxHP > 0 && CurrentHP <= 0;

    // 생성자 수정: isPassableData 인자 추가
    public RuntimeEdge(EdgeType type, CoverType cover, float maxHP, float currentHP, bool isPassableData)
    {
        Type = type;
        Cover = cover;
        MaxHP = maxHP;
        CurrentHP = currentHP;
        _basePassable = isPassableData;
    }

    // [이동 판정] 물리적으로 길을 막고 있는가?
    public bool IsBlocking
    {
        get
        {
            // 1. 아예 뚫린 곳(Open)이면 막지 않음
            if (Type == EdgeType.Open) return false;

            // 2. 벽이 부서졌으면 막지 않음
            if (IsBroken) return false;

            // 3. [Fix] 하드코딩 제거 -> 데이터(_basePassable)에 의존
            // 데이터상 "지나갈 수 있음(Passable)"이면 Blocking은 false
            // 데이터상 "지나갈 수 없음(!Passable)"이면 Blocking은 true
            return !_basePassable;
        }
    }

    // [사격 판정] 투사체가 통과하는가?
    // (이 부분은 기획에 따라 다르겠지만, 보통 Passable하면 사격도 통과함)
    public bool IsPermeable => Type == EdgeType.Open || IsBroken || Type == EdgeType.Window || _basePassable;

    public void TakeDamage(float amount)
    {
        if (MaxHP <= 0 || IsBroken) return; // 파괴 불가능하거나 이미 파괴됨

        CurrentHP -= amount;
        if (CurrentHP < 0) CurrentHP = 0;
    }
}