using Cysharp.Threading.Tasks;
using UnityEngine;

public class PathVisualizer : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }
    public void Initialize() { }
    // 경로 표시용 오버레이 타일 생성 및 제거를 담당합니다.
    public void DrawPath()
    {
        Debug.Log("[PathVisualizer] 경로 시각화 업데이트.");
    }
}