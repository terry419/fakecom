using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button _startExplorationButton;

    private void Start()
    {
        // 버튼 리스너 연결
        if (_startExplorationButton != null)
        {
            _startExplorationButton.onClick.AddListener(OnStartExplorationClicked);
        }
    }

    private void OnStartExplorationClicked()
    {
        Debug.Log("[MainMenu] 탐험을 시작합니다...");
        // 탐험 씬 이름이 "ExplorationScene"이라고 가정
        SceneManager.LoadScene("ExplorationScene");
    }
}