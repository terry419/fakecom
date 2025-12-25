using UnityEngine;

public class PathVisualizer : MonoBehaviour, IInitializable
{
    private void Awake()
    {
        // 1. 깨어날 때 스스로를 등록
        ServiceLocator.Register(this);
        Debug.Log("[Self-Register] TurnManager Registered.");
    }


    private void OnDestroy()
    {
        // 2. 파괴될 때 스스로를 등록 해제 (매우 중요!)
        ServiceLocator.Unregister(this);
        Debug.Log("[Self-Unregister] TurnManager Unregistered.");
    }

    public void Initialize() { }
    // 경로 표시용 오버레이 타일 생성 및 제거를 담당합니다.
    public void DrawPath()
    {
        Debug.Log("[PathVisualizer] 경로 시각화 업데이트.");
    }
}