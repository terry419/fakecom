using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapDataSO))]
public class MapDataValidator : Editor
{
    public override void OnInspectorGUI()
    {
        MapDataSO data = (MapDataSO)target;

        // 경고 메시지 박스 표시 (데이터 불일치 감지 시)
        bool needFix = CheckNeedsFix(data);
        if (needFix)
        {
            EditorGUILayout.HelpBox(" GridSize/BasePosition does not match actual Tile Data!", MessageType.Warning);
            GUI.backgroundColor = Color.yellow;
            if (GUILayout.Button("Auto-Fix Bounds (데이터 기반 재계산)"))
            {
                RecalculateAndSave(data);
            }
            GUI.backgroundColor = Color.white;
        }
        else
        {
            if (GUILayout.Button("Force Recalculate"))
            {
                RecalculateAndSave(data);
            }
        }

        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }

    private bool CheckNeedsFix(MapDataSO data)
    {
        if (data.Tiles == null || data.Tiles.Count == 0) return false;

        // 실제 범위 계산
        CalculateBounds(data, out Vector2Int calculatedBase, out Vector2Int calculatedSize);

        // 현재 값과 비교
        return data.BasePosition != calculatedBase || data.GridSize != calculatedSize;
    }

    private void RecalculateAndSave(MapDataSO data)
    {
        if (data.Tiles == null || data.Tiles.Count == 0) return;

        Undo.RecordObject(data, "Fix Map Bounds");

        CalculateBounds(data, out Vector2Int newBase, out Vector2Int newSize);

        data.BasePosition = newBase;
        data.GridSize = newSize;

        EditorUtility.SetDirty(data);
        Debug.Log($"[MapDataValidator] Fixed {data.name}: Base {newBase}, Size {newSize}");
    }

    private void CalculateBounds(MapDataSO data, out Vector2Int minPos, out Vector2Int size)
    {
        int minX = int.MaxValue;
        int minZ = int.MaxValue;
        int maxX = int.MinValue;
        int maxZ = int.MinValue;

        foreach (var t in data.Tiles)
        {
            if (t.Coords.x < minX) minX = t.Coords.x;
            if (t.Coords.z < minZ) minZ = t.Coords.z;
            if (t.Coords.x > maxX) maxX = t.Coords.x;
            if (t.Coords.z > maxZ) maxZ = t.Coords.z;
        }

        minPos = new Vector2Int(minX, minZ);
        size = new Vector2Int(maxX - minX + 1, maxZ - minZ + 1);
    }
}