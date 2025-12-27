using Cysharp.Threading.Tasks;
using UnityEngine;

public class TilemapGenerator : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }

    public void Initialize() { }
    // 설정된 크기대로 타일을 스폰하고 관리합니다.
    public void Generate()
    {
        Debug.Log("[TilemapGenerator] 타일맵 생성 실행.");
    }
}