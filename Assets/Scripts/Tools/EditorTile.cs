using UnityEngine;

public class EditorTile : MonoBehaviour
{
    [Header("Data")]
    public GridCoords Coordinate;
    public FloorType FloorID;

    // [New] 기둥 데이터를 저장하기 위해 필드 추가
    public PillarType PillarID;

    public SavedEdgeInfo[] Edges = new SavedEdgeInfo[4];

    public void Initialize(GridCoords coords)
    {
        Coordinate = coords;
        FloorID = FloorType.Concrete;
        PillarID = PillarType.None; // [New] 초기화

        // 1. 초기화: 일단 모두 Open으로 설정
        if (Edges == null || Edges.Length != 4) Edges = new SavedEdgeInfo[4];
        for (int i = 0; i < 4; i++) Edges[i] = SavedEdgeInfo.CreateOpen();

        // 2. 최적화된 동기화 로직 실행
        SyncWithNeighboringWalls();

        UpdateName();
    }

    /// <summary>
    /// [최적화됨] 씬의 모든 벽을 1회 순회하며(O(N)), 나와 연관된 벽만 필터링하여 데이터를 동기화합니다.
    /// </summary>
    public void SyncWithNeighboringWalls()
    {
        // FindObjectsOfType은 무거운 연산이므로 루프 밖에서 단 1회만 호출합니다.
        EditorWall[] allWalls = FindObjectsOfType<EditorWall>();
        if (allWalls == null || allWalls.Length == 0) return;

        foreach (var wall in allWalls)
        {
            // Case 1: 벽이 '내 좌표' 위에 있고, '내 엣지'인 경우
            if (wall.Coordinate == this.Coordinate)
            {
                Edges[(int)wall.Direction] = CreateEdgeDataFromWall(wall);
                continue; // 찾았으니 다음 벽으로 넘어감
            }

            // Case 2: 벽이 '옆 타일'에 있고, '나를 바라보는' 경우
            // (벽의 좌표 + 벽의 방향 = 내 좌표)인 경우입니다.
            // GridUtils.GetNeighbor를 사용하여 벽이 바라보는 앞 칸 좌표를 구합니다
            GridCoords wallFacingCoords = GridUtils.GetNeighbor(wall.Coordinate, wall.Direction);

            if (wallFacingCoords == this.Coordinate)
            {
                // 벽이 나를 보고 있다면, 내 입장에서의 방향은 '벽의 반대 방향'입니다
                Direction myEdgeDir = GridUtils.GetOppositeDirection(wall.Direction);
                Edges[(int)myEdgeDir] = CreateEdgeDataFromWall(wall);
            }
        }
    }

    private SavedEdgeInfo CreateEdgeDataFromWall(EditorWall wall)
    {
        switch (wall.Type)
        {
            case EdgeType.Wall: return SavedEdgeInfo.CreateWall(wall.DataType, wall.MaxHP, wall.Cover);
            case EdgeType.Window: return SavedEdgeInfo.CreateWindow(wall.DataType, wall.MaxHP, wall.Cover);
            case EdgeType.Door: return SavedEdgeInfo.CreateDoor(wall.DataType, wall.MaxHP, wall.Cover);
            default: return SavedEdgeInfo.CreateOpen();
        }
    }

    public void UpdateName()
    {
        gameObject.name = $"Tile_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}";
    }

    private void OnValidate()
    {
        if (!Application.isPlaying) UpdateName();
    }
}