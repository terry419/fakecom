using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Collections.Generic;

[Serializable]
public struct MapEntry
{
    [Header("1. Identity")]
    public string MapID;
    public string DisplayName;

    [Header("2. Mission Details")]
    public MissionType Type;
    public MapSize Size;

    [TextArea(3, 5)]
    public string Description;

    [Header("3. Environment")]
    public List<string> Tags;

    [Header("4. Asset Link")]
    public AssetReferenceT<MapDataSO> MapDataRef;

    public bool Validate(out string error)
    {
        // 필수 ID 검사
        if (string.IsNullOrEmpty(MapID))
        {
            error = "MapID is empty.";
            return false;
        }
        if (string.IsNullOrEmpty(DisplayName))
        {
            error = $"DisplayName is empty (MapID: {MapID})";
            return false;
        }

        // [개선 1] Enum 유효성 검사
        if (!Enum.IsDefined(typeof(MissionType), Type))
        {
            error = $"Invalid MissionType enum value in {MapID}";
            return false;
        }
        if (!Enum.IsDefined(typeof(MapSize), Size))
        {
            error = $"Invalid MapSize enum value in {MapID}";
            return false;
        }

        // [개선 6] 설명 누락 경고 (Error 아님 -> true 반환)
        if (string.IsNullOrEmpty(Description))
        {
            Debug.LogWarning($"[MapEntry] Warning: Description is empty for {MapID}");
        }

        if (!MapDataRef.RuntimeKeyIsValid())
        {
            error = $"Invalid Addressable Key (MapID: {MapID})";
            return false;
        }

        error = string.Empty;
        return true;
    }
}