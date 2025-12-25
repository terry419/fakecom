using UnityEngine;

public class GameManager : MonoBehaviour
{
    public bool IsPaused { get; private set; } = false;

    public void Initialize()
    {
        // 게임 상태 설정 초기화
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