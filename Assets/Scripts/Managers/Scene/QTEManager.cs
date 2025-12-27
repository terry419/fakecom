using Cysharp.Threading.Tasks;
using UnityEngine;

public class QTEManager : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }

    public void Initialize() { }
    // QTE 시작 요청 및 결과 반환 로직이 들어올 공간입니다.
    public void StartQTE()
    {
        Debug.Log("[QTEManager] QTE 시스템 가동.");
    }
}