using Cysharp.Threading.Tasks;
using UnityEngine;

public class TargetUIManager : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }
    public void Initialize() { }
    // 타겟 정보 UI 표시 및 풀링을 담당합니다.
    public void ShowTargetInfo()
    {
        Debug.Log("[TargetUIManager] 타겟 UI 표시.");
    }
}