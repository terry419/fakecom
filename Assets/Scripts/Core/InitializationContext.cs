using UnityEngine;
using System; // Exception 사용

public class InitializationContext
{
    // [필수] 전역 환경 설정 (소리, 그래픽)
    public GlobalSettingsSO GlobalSettings { get; set; }

    // [필수] 맵 비주얼 설정 (타일/벽 프리팹 매핑 정보)
    public MapEditorSettingsSO MapVisualSettings { get; set; }

    // [필수] 매니저 스코프
    public ManagerScope Scope { get; set; }

    // [선택] 맵 데이터
    public MapDataSO MapData { get; set; } = null;

    // [선택] 세이브 데이터
    public ISaveData UserData { get; set; } = null;

    // 헬퍼 프로퍼티
    public bool HasMapData => MapData != null;
    public bool HasMapVisualSettings => MapVisualSettings != null;

    /// <summary>
    /// [New] Global Scope 초기화 시 필수 설정들이 모두 제공되었는지 엄격하게 검증합니다.
    /// 하나라도 누락되면 게임을 시작할 수 없습니다.
    /// </summary>
    public void ValidateGlobalSettings()
    {
        if (GlobalSettings == null)
            throw new InvalidOperationException("[InitializationContext] CRITICAL: GlobalSettings is null.");

        if (MapVisualSettings == null)
            throw new InvalidOperationException("[InitializationContext] CRITICAL: MapVisualSettings is null. TileDataManager cannot function without it.");

        // Scope 확인 (Global 초기화인데 Scene Scope로 설정된 경우 등 방지)
        if (Scope != ManagerScope.Global)
            Debug.LogWarning($"[InitializationContext] Warning: Validating Global Settings but Scope is {Scope}.");
    }
}