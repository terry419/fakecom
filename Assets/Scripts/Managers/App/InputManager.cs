using UnityEngine;

public class InputManager : MonoBehaviour
{
    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    // 유니티 Input System 또는 기존 Input 입력을 처리합니다.
    public void EnableInput(bool enabled)
    {
        Debug.Log($"[InputManager] 입력 활성화 상태: {enabled}");
    }
}