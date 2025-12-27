using Cysharp.Threading.Tasks;
using UnityEngine;

public class DamageTextManager : MonoBehaviour, IInitializable
{
    public async UniTask Initialize(InitializationContext context)
    {
        // 지금은 할 일이 없으니 바로 완료 보고
        await UniTask.CompletedTask;
    }

    public void Initialize() { }
    // 데미지 텍스트 프리팹 생성 및 연출을 관리합니다.
    public void PopDamageText()
    {
        Debug.Log("[DamageTextManager] 데미지 텍스트 출력.");
    }
}