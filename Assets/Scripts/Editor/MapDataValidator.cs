using UnityEngine;
using UnityEditor;
using System.Text;
using System.Collections.Generic;

[CustomEditor(typeof(MapDataSO))]
public class MapDataValidator : Editor
{
    public override void OnInspectorGUI()
    {
        MapDataSO data = (MapDataSO)target;

        if (CheckBoundsError(data))
        {
            EditorGUILayout.HelpBox("GridSize/BasePosition mismatch!", MessageType.Warning);
            if (GUILayout.Button("Auto-Fix Bounds")) RecalculateAndSave(data);
        }

        string portalError = ValidatePortalData(data);
        if (!string.IsNullOrEmpty(portalError))
        {
            EditorGUILayout.HelpBox($"Portal Errors Found:\n{portalError}", MessageType.Error);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Auto-Fix Invalid Portals (Remove Bad Links)"))
            {
                FixInvalidPortals(data);
            }
            GUI.backgroundColor = Color.white;
        }

        EditorGUILayout.Space();
        base.OnInspectorGUI();
    }

    private bool CheckBoundsError(MapDataSO data)
    {
        if (data.Tiles == null || data.Tiles.Count == 0) return false;
        CalculateBounds(data, out Vector2Int basePos, out Vector2Int size);
        return data.BasePosition != basePos || data.GridSize != size;
    }

    private string ValidatePortalData(MapDataSO data)
    {
        if (data.Tiles == null) return string.Empty;

        StringBuilder sb = new StringBuilder();
        HashSet<GridCoords> validCoords = new HashSet<GridCoords>();
        foreach (var t in data.Tiles) validCoords.Add(t.Coords);

        foreach (var tile in data.Tiles)
        {
            if (tile.PortalData != null && tile.PortalData.Destinations != null)
            {
                for (int i = 0; i < tile.PortalData.Destinations.Count; i++)
                {
                    // [Fix] 구조체에서 좌표 추출
                    var destStruct = tile.PortalData.Destinations[i];
                    GridCoords destCoord = destStruct.Coordinate;

                    if (!validCoords.Contains(destCoord))
                        sb.AppendLine($"- Tile {tile.Coords}: Dest[{i}] {destCoord} is out of map.");

                    if (tile.Coords.Equals(destCoord))
                        sb.AppendLine($"- Tile {tile.Coords}: Dest[{i}] points to itself.");
                }
            }
        }
        return sb.ToString();
    }

    private void FixInvalidPortals(MapDataSO data)
    {
        if (data.Tiles == null) return;
        Undo.RecordObject(data, "Fix Invalid Portals");

        HashSet<GridCoords> validCoords = new HashSet<GridCoords>();
        foreach (var t in data.Tiles) validCoords.Add(t.Coords);

        int fixedCount = 0;

        for (int i = 0; i < data.Tiles.Count; i++)
        {
            var tile = data.Tiles[i];

            if (tile.PortalData != null && tile.PortalData.Destinations != null)
            {
                int initialCount = tile.PortalData.Destinations.Count;

                // [Fix] RemoveAll 람다식에서 구조체 내부 좌표 비교
                tile.PortalData.Destinations.RemoveAll(destStruct =>
                    !validCoords.Contains(destStruct.Coordinate) || destStruct.Coordinate.Equals(tile.Coords));

                if (tile.PortalData.Destinations.Count == 0)
                {
                    tile.PortalData = null;
                    fixedCount++;
                }
                else if (tile.PortalData.Destinations.Count != initialCount)
                {
                    fixedCount++;
                }

                data.Tiles[i] = tile;
            }
        }

        if (fixedCount > 0)
        {
            EditorUtility.SetDirty(data);
            Debug.Log($"[MapDataValidator] Fixed {fixedCount} tiles with invalid portal links.");
        }
    }

    private void RecalculateAndSave(MapDataSO data)
    {
        Undo.RecordObject(data, "Fix Map Bounds");
        CalculateBounds(data, out Vector2Int newBase, out Vector2Int newSize);
        data.BasePosition = newBase;
        data.GridSize = newSize;
        EditorUtility.SetDirty(data);
    }

    private void CalculateBounds(MapDataSO data, out Vector2Int minPos, out Vector2Int size)
    {
        int minX = int.MaxValue; int minZ = int.MaxValue;
        int maxX = int.MinValue; int maxZ = int.MinValue;

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