/// <summary>
/// 세션의 상태를 정의하는 열거형입니다.
/// </summary>
public enum SessionState
{
    None,           // 초기화 전 상태
    Boot,           // 필수 데이터 로드 및 시스템 준비 단계
    Setup,          // 맵 생성, 유닛 배치 등 전투 준비 단계

    // 턴 기반 전투 흐름
    TurnWaiting,    // 다음 행동 유닛을 계산하는 대기 상태
    PlayerTurn,     // 플레이어가 유닛을 조작할 수 있는 상태
    UnitTurn,       // 특정 유닛(플레이어 또는 적)의 행동이 진행되는 상태
    
    // 시네마틱 및 전투 결과
    Cinematic,      // 컷씬 등 연출이 진행되는 상태
    BattleEnd,      // 전투가 종료되고 승/패 연출이 표시되는 상태

    // 기타 상태
    GameLoop,       // 실시간 게임 루프 (사용될 경우)
    Dialogue,       // NPC 등과 대화하는 상태
    SystemOption,   // 시스템 메뉴(옵션, 저장 등)가 활성화된 상태
    Saving,         // 게임 저장 상태
    Retry,          // 재시도 상태

    // 마무리
    Resolution,     // 전투 결과(보상, 점수)를 정산하고 보여주는 상태
    Cleanup,        // 씬을 정리하고 메모리를 해제하는 단계
    Error           // 치명적인 오류가 발생하여 게임 진행이 불가능한 상태
}