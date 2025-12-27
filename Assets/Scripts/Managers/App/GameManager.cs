using UnityEngine;
using Cysharp.Threading.Tasks; // 비동기 필수

public class GameManager : MonoBehaviour, IInitializable
{
    public bool IsPaused { get; private set; } = false;

    // 1. 태어날 때: 공구함(ServiceLocator)에 등록만 함
    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
        Debug.Log($"[GameManager] 등록 완료 (Global).");
    }

    // 2. 죽을 때: 등록 해제
    private void OnDestroy()
    {
        ServiceLocator.Unregister<GameManager>(ManagerScope.Global);
    }

    // 3. 시동 걸 때: 여기서 진짜 준비를 함
    public async UniTask Initialize(InitializationContext context)
    {
        // 예: 그래픽 설정 적용
        Application.targetFrameRate = context.GlobalSettings.TargetFrameRate;

        Debug.Log("[GameManager] 초기화 중... 설정 적용 완료.");
        await UniTask.CompletedTask; // 특별히 기다릴 게 없으면 바로 완료 보고
    }

    // 게임 시작/정지 기능
    public void StartGame()
    {
        Debug.Log("[GameManager] 게임 시작!");
        IsPaused = false;
        Time.timeScale = 1.0f;
    }

    public void PauseGame()
    {
        Debug.Log("[GameManager] 일시 정지.");
        IsPaused = true;
        Time.timeScale = 0.0f;
    }
}