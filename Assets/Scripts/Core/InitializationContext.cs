using UnityEngine;
using System;

public class InitializationContext
{
    // [필수] 전역 환경 설정
    public GlobalSettingsSO GlobalSettings { get; set; }

    // [Mod] 맵 비주얼 설정 -> 통합 레지스트리 (Visual + Data)
    public TileRegistrySO Registry { get; set; }

    // [New] 맵 카탈로그 (AppBootstrapper에서 로드하여 주입)
    public MapCatalogSO MapCatalog { get; set; }

    // [필수] 매니저 스코프
    public ManagerScope Scope { get; set; }

    // [선택] 데이터들 (Scene Scope 등에서 사용)
    public MapDataSO MapData { get; set; } = null;
    public ISaveData UserData { get; set; } = null;

    // 편의 프로퍼티
    public bool HasMapData => MapData != null;
    public bool HasRegistry => Registry != null;

    public void ValidateGlobalSettings()
    {
        if (GlobalSettings == null)
            throw new InvalidOperationException("[InitializationContext] CRITICAL: GlobalSettings is null.");

        if (Registry == null)
            throw new InvalidOperationException("[InitializationContext] CRITICAL: TileRegistry is null. TileDataManager cannot function without it.");

        // [New] Global Scope인데 카탈로그가 없으면 경고
        if (Scope == ManagerScope.Global && MapCatalog == null)
        {
            Debug.LogWarning("[InitializationContext] Warning: MapCatalog is null in Global Scope. Check AppBootstrapper.");
        }

        if (Scope != ManagerScope.Global)
        {
            // Global이 아닌데 GlobalSettings를 검증하려 할 때 경고 (선택 사항)
            // Debug.LogWarning($"[InitializationContext] Warning: Validating Global Settings but Scope is {Scope}.");
        }
    }
}