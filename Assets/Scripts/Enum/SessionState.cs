    /// <summary>
    /// 게임 세션의 라이프사이클 상태 목록
    /// </summary>
    public enum SessionState
    {
        None,           // 초기화 전
        Boot,           // 데이터 로드, 시스템 준비
        Setup,          // 맵 생성, 유닛 배치 (화면 가림막 유지)

        // 전투 루프 세분화
        TurnWaiting,    //  다음 TS 도달 유닛을 기다리는 상태
        UnitTurn,       //  특정 유닛이 행동 중인 상태 (입력 허용)


        Cinematic,      // 오프닝 연출, 보스 등장 (입력 차단, 시간 흐름)
        GameLoop,       // 메인 게임 진행 (입력 허용)
        Dialogue,       // NPC 대화 중 (이동 불가, 대화 넘기기 가능)
        SystemOption,   // 일시 정지 (모든 시간 정지, 시스템 메뉴 조작)
        Saving,         //세이브 중
        Retry,          //패배 후 다시 하기


        Resolution,     // 승패 판정 및 결과 연출
        Cleanup,        // 씬 종료 및 메모리 정리
        Error           // 치명적 오류 대응
    }
