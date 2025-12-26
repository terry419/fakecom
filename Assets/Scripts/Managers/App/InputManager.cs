using UnityEngine;

public class InputManager : MonoBehaviour
{
    private void Awake()
    {
        // [자가 등록]
        ServiceLocator.Register(this);
        Debug.Log($"[Self-Register] {nameof(InputManager)} Registered.");
    }

    private void OnDestroy()
    {
        // [자가 해제]
        ServiceLocator.Unregister<InputManager>();
    }

    public void EnableInput(bool enabled)
    {
        Debug.Log($"[InputManager] 입력 활성화 상태 : {enabled}");
    }
}