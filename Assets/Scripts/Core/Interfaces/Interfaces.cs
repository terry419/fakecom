using Cysharp.Threading.Tasks;

namespace Core
{
    // 초기화 인터페이스 (비동기)
    public interface IInitializable
    {
        UniTask Initialize(InitializationContext context);
    }

    // 정리 인터페이스 (비동기, 선택적 구현)
    // 리소스 해제, 파일 저장 등 시간이 걸리는 작업 대응
    public interface ICleanup
    {
        UniTask Cleanup();
    }

    // 세이브 데이터 추상화
    public interface ISaveData
    {
        // 최소 데이터 정의 (예: 식별자)
    }

    // 초기화 컨텍스트
    public class InitializationContext
    {
        public GlobalSettingsSO GlobalSettings { get; set; }
        public ManagerScope Scope { get; set; }
        public MapDataSO MapData { get; set; } = null;
        public ISaveData UserData { get; set; } = null;

        public bool HasMapData => MapData != null;
    }
}