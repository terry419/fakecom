using UnityEngine;
using Cysharp.Threading.Tasks;

public class DataManager : MonoBehaviour, IInitializable
{
    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<DataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {

        // 나중에 여기서 무기 데이터, 적 데이터를 로드하는 데 1~2초 걸린다고 가정
        // await LoadAllGameDataAsync(); 

        await UniTask.CompletedTask;
    }
}