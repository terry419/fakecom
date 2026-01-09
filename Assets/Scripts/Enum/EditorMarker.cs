using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// [Fix] Enum 정의 삭제 (TileRegistrySO로 이동됨)

[ExecuteInEditMode]
public class EditorMarker : MonoBehaviour
{
    [Header("Common Settings")]
    public MarkerType MarkerCategory; // Spawn or Portal
    public string ID;

    [Header("Spawn Settings")]
    // [New] 스폰 전용 하위 타입
    public SpawnType SType;

    [Header("Portal Settings")]
    public PortalType PType;
    public Direction Facing = Direction.North;

    [Header("Visuals")]
    public Color GizmoColor = Color.white;

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawSphere(transform.position, 0.3f);

        // 출구 포탈일 때 방향 화살표 표시
        if (MarkerCategory == MarkerType.Portal && PType == PortalType.Out)
        {
            Gizmos.color = Color.yellow;
            GridCoords dirCoords = GridUtils.GetDirectionVector(Facing);
            Vector3 dirVec = new Vector3(dirCoords.x, 0, dirCoords.z).normalized;

            Gizmos.DrawRay(transform.position, dirVec * 1.0f);

            // 화살표 머리
            Vector3 right = Quaternion.LookRotation(dirVec) * Quaternion.Euler(0, 150, 0) * Vector3.forward;
            Vector3 left = Quaternion.LookRotation(dirVec) * Quaternion.Euler(0, -150, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position + dirVec, right * 0.3f);
            Gizmos.DrawRay(transform.position + dirVec, left * 0.3f);
        }

        // 씬 뷰 텍스트 표시
#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;

        string info = $"{MarkerCategory}\nID: {ID}";

        // [New] 카테고리에 따라 세부 정보 표시 분기
        if (MarkerCategory == MarkerType.Portal)
            info += $"\n({PType}) -> {Facing}";
        else if (MarkerCategory == MarkerType.Spawn)
            info += $"\n({SType})";

        Handles.Label(transform.position + Vector3.up * 0.8f, info, style);
#endif

        // 포탈 연결선 (In -> Out)
        if (MarkerCategory == MarkerType.Portal && PType == PortalType.In)
        {
            DrawConnection();
        }
    }

    private void DrawConnection()
    {
        var all = FindObjectsOfType<EditorMarker>();
        foreach (var m in all)
        {
            // 같은 ID를 가진 출구 포탈과 연결
            if (m != this && m.MarkerCategory == MarkerType.Portal && m.PType == PortalType.Out && m.ID == this.ID)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, m.transform.position);
            }
        }
    }
}