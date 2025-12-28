using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// [Step 1.5 수정] 데이터 동기화 로직 제거.
/// 단순히 씬에 배치된 벽의 정보(타입, 스탯)를 담고 있는 컨테이너 역할만 수행함.
/// 모든 데이터 제어 권한은 MapEditorTool에게 있음.
/// </summary>
public class EditorWall : MonoBehaviour
{
    [Header("Identity")]
    public GridCoords Coordinate;
    public Direction Direction;

    [Header("Data")]
    public EdgeType Type;
    public EdgeDataType DataType;
    public CoverType Cover;
    public float MaxHP;
    public float CurrentHP;

    /// <summary>
    /// MapEditorTool에서 생성 직후 호출하여 데이터를 주입함.
    /// </summary>
    public void Initialize(GridCoords coords, Direction dir, SavedEdgeInfo edgeInfo)
    {
        Coordinate = coords;
        Direction = dir;

        Type = edgeInfo.Type;
        DataType = edgeInfo.DataType;
        Cover = edgeInfo.Cover;
        MaxHP = edgeInfo.MaxHP;
        CurrentHP = edgeInfo.CurrentHP;

        UpdateName();
    }

    public void UpdateName()
    {
        gameObject.name = $"Edge_{Coordinate.x}_{Coordinate.z}_{Coordinate.y}_{Direction}";
    }

    private void OnValidate()
    {
        // 인스펙터에서 수정 시 이름만 갱신 (데이터 동기화 로직 제거됨)
        if (!Application.isPlaying) UpdateName();
    }

    // OnDestroy, OnEnable 삭제됨 -> 데이터 오염 방지
}