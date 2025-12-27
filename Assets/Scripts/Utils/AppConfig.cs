using UnityEngine;
using UnityEngine.AddressableAssets; // 어드레서블(리소스 관리) 기능 사용

// 우클릭 -> Create -> System -> AppConfig 로 이 파일을 만들 수 있게 해줍니다.
[CreateAssetMenu(fileName = "AppConfig", menuName = "System/AppConfig")]
public class AppConfig : ScriptableObject
{
    [Header("UI 리소스")]
    // 로딩 중에 띄울 화면 (프리팹)
    public AssetReferenceGameObject BootCanvasRef;

    [Header("필수 매니저 (Global)")]
    public AssetReferenceT<GlobalSettingsSO> GlobalSettingsRef;
    // 게임 끄기 전까지 절대 죽지 않는 매니저들
    public AssetReferenceGameObject GameManagerRef;    // 게임 총괄
    public AssetReferenceGameObject DataManagerRef;    // 데이터 관리
    public AssetReferenceGameObject InputManagerRef;   // 입력(키보드/마우스) 관리
    public AssetReferenceGameObject SaveManagerRef;    // 저장/불러오기 관리
}