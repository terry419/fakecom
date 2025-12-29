using Cysharp.Threading.Tasks;

// [Refactoring Phase 1] P0: 인터페이스 설계 중복 제거
// 동기식 void Initialize()를 제거하고, 비동기 초기화만 강제합니다.
public interface IInitializable
{
    UniTask Initialize(InitializationContext context);
}