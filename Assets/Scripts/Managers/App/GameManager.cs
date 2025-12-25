using UnityEngine;

public class GameManager : MonoBehaviour
{
    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    public bool IsPaused { get; private set; } = false;

    public void Initialize()
    {
        // ���� ���� ���� �ʱ�ȭ
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