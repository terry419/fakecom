using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "AppConfig", menuName = "System/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("Core Data")]
    // GlobalSettings는 데이터이므로 에셋 레퍼런스로 유지합니다.
    [SerializeField] private AssetReferenceT<GlobalSettingsSO> _globalSettingsRef;

    // 매니저 프리팹 레퍼런스(GameObject)들은 모두 삭제했습니다.
    // 더 이상 인스펙터에 일일이 넣을 필요 없습니다.

    // Getter
    public AssetReferenceT<GlobalSettingsSO> GlobalSettingsRef => _globalSettingsRef;
}