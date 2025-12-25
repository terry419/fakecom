using UnityEngine;

public class MapManager : MonoBehaviour, IInitializable
{
    public void Initialize() { } // <-- 추가
    // 타일 조회, 경로 계산(A*) 등의 로직이 들어올 공간입니다.
    public void InitMap()
    {
        Debug.Log("[MapManager] 맵 데이터 초기화 준비.");
    }
}

