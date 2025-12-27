using UnityEngine;

public class InitializationContext
{
    // [필수] 게임 전체 설정 (소리, 언어 등)
    public GlobalSettingsSO GlobalSettings { get; set; }

    // [필수] 내 신분 (Global인지 Scene인지)
    public ManagerScope Scope { get; set; }

    // [선택] 맵 데이터 (전투 씬에서만 들어있음)
    public MapDataSO MapData { get; set; } = null;

    // [선택] 세이브 데이터 (로드한 게임일 때만 들어있음)
    public ISaveData UserData { get; set; } = null;

    // 가방 안에 맵 데이터가 들어있는지 확인하는 기능
    public bool HasMapData => MapData != null;
}