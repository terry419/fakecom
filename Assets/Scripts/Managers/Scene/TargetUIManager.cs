using UnityEngine;

public class TargetUIManager : MonoBehaviour, IInitializable
{
    public void Initialize() { }
    // 타겟 정보 UI 표시 및 풀링을 담당합니다.
    public void ShowTargetInfo()
    {
        Debug.Log("[TargetUIManager] 타겟 UI 표시.");
    }
}