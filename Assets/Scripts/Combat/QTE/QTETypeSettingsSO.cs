using UnityEngine;
using System;

[CreateAssetMenu(fileName = "QTETypeSettings", menuName = "YCOM/Combat/QTE Type Settings")]
public class QTETypeSettingsSO : ScriptableObject
{
    [System.Serializable]
    public struct QTETypeData
    {
        public QTEType type;
        [Range(0, 100)] public float hitChance;
        [Range(0, 100)] public float critChance;
    }

    [SerializeField] private QTETypeData[] typeSettings;

    public (float hitChance, float critChance) GetChances(QTEType type)
    {
        if (typeSettings == null) return (50f, 20f);

        foreach (var data in typeSettings)
        {
            if (data.type == type)
                return (data.hitChance, data.critChance);
        }

        // 데이터가 없으면 기본값 반환
        return (50f, 20f);
    }
}