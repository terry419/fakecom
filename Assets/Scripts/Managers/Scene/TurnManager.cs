using Cysharp.Threading.Tasks;
using UnityEngine;

public class TurnManager : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }
    public void Initialize() { }
    // 나중에 턴 순서, 제한 시간 등을 여기서 관리하겠죠?
    public void StartBattle()
    {
        Debug.Log("전투 시작! 턴 시스템 가동.");
    }
}