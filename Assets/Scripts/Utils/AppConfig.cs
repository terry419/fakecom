using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AppConfig", menuName = "System/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("UI 리소스")]
    public AssetReferenceGameObject BootCanvasRef;

    [Header("Global Managers (DontDestroyOnLoad)")]
    public AssetReferenceT<GlobalSettingsSO> GlobalSettingsRef;

    // [New] 맵 비주얼 설정 (TileDataManager 주입용)
    public AssetReferenceT<MapEditorSettingsSO> MapVisualSettingsRef;

    // 시스템 매니저
    public AssetReferenceGameObject GameManagerRef;
    public AssetReferenceGameObject DataManagerRef;
    public AssetReferenceGameObject InputManagerRef;
    public AssetReferenceGameObject SaveManagerRef;

    // [추가] 리소스 매니저 (Global Scope)
    public AssetReferenceGameObject EdgeDataManagerRef;
    public AssetReferenceGameObject TileDataManagerRef;
}