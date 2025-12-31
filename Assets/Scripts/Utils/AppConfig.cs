using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AppConfig", menuName = "System/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("UI 리소스")]
    public AssetReferenceGameObject BootCanvasRef;

    [Header("Global Managers (DontDestroyOnLoad)")]
    public AssetReferenceT<GlobalSettingsSO> GlobalSettingsRef;

    // [Mod] MapEditorSettingsSO -> TileRegistrySO 로 교체
    // 기존: public AssetReferenceT<MapEditorSettingsSO> MapVisualSettingsRef;
    public AssetReferenceT<TileRegistrySO> TileRegistryRef;

    // 시스템 매니저 (기존 유지)
    public AssetReferenceGameObject GameManagerRef;
    public AssetReferenceGameObject DataManagerRef;
    public AssetReferenceGameObject InputManagerRef;
    public AssetReferenceGameObject SaveManagerRef;

    // 리소스 매니저 (기존 유지)
    public AssetReferenceGameObject EdgeDataManagerRef;
    public AssetReferenceGameObject TileDataManagerRef;
}