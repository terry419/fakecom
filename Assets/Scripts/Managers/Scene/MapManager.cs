using Cysharp.Threading.Tasks;
using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }

    public void Initialize() { } // <-- 추가
    // 타일 조회, 경로 계산(A*) 등의 로직이 들어올 공간입니다.
    public void InitMap()
    {
        Debug.Log("[MapManager] 맵 데이터 초기화 준비.");
    }
}

