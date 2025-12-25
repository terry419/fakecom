using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AppConfig", menuName = "System/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("1. Core Data (Addressable)")]
    // [중요] 타입 안정성을 위해 AssetReferenceT 사용
    [SerializeField] private AssetReferenceT<GlobalSettingsSO> _globalSettingsRef;

    [Header("2. Global Managers (Addressable)")]
    [SerializeField] private AssetReferenceT<GameObject> _inputManagerRef;

    [SerializeField] private AssetReferenceT<GameObject> _dataManagerRef;
    [SerializeField] private AssetReferenceT<GameObject> _gameManagerRef;

#if UNITY_EDITOR
    [Header("Editor Debug Info")]
    [SerializeField] public GlobalSettingsSO _editorGlobalSettingsDebug;

#endif

    // -----------------------------------------------------------------------
    // [핵심] AppInitializer가 갖다 쓸 수 있게 '열쇠(Getter)'를 줘야 함
    // -----------------------------------------------------------------------
    public AssetReferenceT<GlobalSettingsSO> GlobalSettingsRef => _globalSettingsRef;
    public AssetReferenceT<GameObject> InputManagerRef => _inputManagerRef;
    public AssetReferenceT<GameObject> DataManagerRef => _dataManagerRef;
   public AssetReferenceT<GameObject> GameManagerRef => _gameManagerRef;
}