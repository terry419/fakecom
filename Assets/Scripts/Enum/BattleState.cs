public enum BattleState
{
    None,
    Setup,          // 맵 생성, 유닛 배치
    TurnWaiting,    // 턴 계산 대기
    PlayerTurn,     // 플레이어 조작
    UnitTurn,       // 유닛(AI/Player) 행동 실행
    BattleEnd,      // 전투 종료 연출
    Resolution,     // 보상 및 정산
    Error           // 전투 중 치명적 오류
}