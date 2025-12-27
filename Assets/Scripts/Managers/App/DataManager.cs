using UnityEngine;
using Cysharp.Threading.Tasks;

public class DataManager : MonoBehaviour, IInitializable
{
    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
        Debug.Log($"[DataManager] 등록 완료 (Global).");
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<DataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        Debug.Log("[DataManager] 데이터 로딩 시작...");

        // 나중에 여기서 무기 데이터, 적 데이터를 로드하는 데 1~2초 걸린다고 가정
        // await LoadAllGameDataAsync(); 

        await UniTask.CompletedTask;
        Debug.Log("[DataManager] 모든 데이터 로드 완료.");
    }
}