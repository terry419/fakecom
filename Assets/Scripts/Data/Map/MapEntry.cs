// 파일: Assets/Scripts/Data/Map/MapEntry.cs
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

    // [Mod] 선택 사항으로 변경 (Null 허용 -> 기본 타일셋 사용)
    [Header("5. Visual Theme (Optional)")]
    public AssetReferenceT<TileRegistrySO> BiomeRegistryRef;

    public bool Validate(out string error)
    {
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

        // 맵 데이터는 필수 (지형이 없으면 게임 불가)
        if (MapDataRef == null || !MapDataRef.RuntimeKeyIsValid())
        {
            error = $"Invalid or Missing MapDataRef in MapID: {MapID}";
            return false;
        }

        // [Mod] BiomeRegistryRef는 검사하지 않음 (Null이면 Fallback 사용)

        error = string.Empty;
        return true;
    }
}