// 파일명: UnitCondition.cs
public enum UnitCondition
{
    Hopeful,        // 희망적 (행동력 보너스 or TS 감소)
    Inspired,       // 고양됨 (소폭 보너스)
    Normal,         // 일반
    Incapacitated,  // 행동 불능 (기절 등)
    Fleeing,        // 패닉/도주 (제어 불가)
    FriendlyFire,   // 피아식별 불가 (아군 공격 위험)
    SelfHarm        // 자해 (스스로 공격)
}