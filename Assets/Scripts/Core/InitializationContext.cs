using UnityEngine;
using System;

public class InitializationContext
{
    // [필수] 전역 환경 설정
    public GlobalSettingsSO GlobalSettings { get; set; }

    // [Mod] 맵 비주얼 설정 -> 통합 레지스트리 (Visual + Data)
    // 기존: public MapEditorSettingsSO MapVisualSettings { get; set; }
    public TileRegistrySO Registry { get; set; }

    // [필수] 매니저 스코프
    public ManagerScope Scope { get; set; }

    // [선택] 데이터들
    public MapDataSO MapData { get; set; } = null;
    public ISaveData UserData { get; set; } = null;

    public bool HasMapData => MapData != null;

    // [Mod] 이름 변경
    public bool HasRegistry => Registry != null;

    public void ValidateGlobalSettings()
    {
        if (GlobalSettings == null)
            throw new InvalidOperationException("[InitializationContext] CRITICAL: GlobalSettings is null.");

        // [Mod] 유효성 검사 대상 변경
        if (Registry == null)
            throw new InvalidOperationException("[InitializationContext] CRITICAL: TileRegistry is null. TileDataManager cannot function without it.");

        if (Scope != ManagerScope.Global)
            Debug.LogWarning($"[InitializationContext] Warning: Validating Global Settings but Scope is {Scope}.");
    }
}