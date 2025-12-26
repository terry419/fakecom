using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        ServiceLocator.Register(this);
        Debug.Log($"[Self-Register] {nameof(GameManager)} Registered.");
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<GameManager>();
    }

    public void Initialize()
    {
        // 게임 상태 초기화
    }

    public void StartGame()
    {
        Debug.Log("[GameManager] Game State: Playing");
        IsPaused = false;
        Time.timeScale = 1.0f;
    }

    public void StopGame()
    {
        Debug.Log("[GameManager] Game State: Stopped");
        IsPaused = false;
        Time.timeScale = 1.0f;
    }
}