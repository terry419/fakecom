// UniTask(비동기)를 쓰기 위해 필요합니다.
using Cysharp.Threading.Tasks;

// interface: "이 기능을 가진 애들은 무조건 아래 함수를 가지고 있어야 해!" 라는 약속장입니다.
public interface ICleanup
{
    // "청소해!" 함수입니다.
    // 반환값이 void가 아니라 UniTask인 이유: 
    // 청소가 0.1초 만에 끝날 수도 있지만, 3초가 걸릴 수도 있습니다(파일 저장 등).
    // "청소가 다 끝날 때까지 기다려줄게"라고 하기 위해 UniTask를 씁니다.
    UniTask Cleanup();
}
    
