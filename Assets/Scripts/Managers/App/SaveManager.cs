using UnityEngine;
using Cysharp.Threading.Tasks;
using System.IO; // 파일 입출력용

public class SaveManager : MonoBehaviour, IInitializable
{
    private string _savePath;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SaveManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 저장 경로 설정 (C:/Users/AppData/...)

        // 마지막 세이브 파일이 있는지 확인하는 로직 등이 여기에 들어갑니다.
        await UniTask.CompletedTask;
    }
}