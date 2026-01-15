using UnityEngine;
using System.Collections.Generic;


public enum QTEResult { None, Success, Miss, Timeout }
public enum QTEGrade { Miss = 0, Graze = 1, Hit = 2, Critical = 3 }

[System.Serializable]
public struct ZoneInfo
{
    public QTEGrade Grade;
    public float StartMin;
    public float EndMax;

    public ZoneInfo(QTEGrade grade, float start, float end)
    {
        Grade = grade;
        StartMin = start;
        EndMax = end;
    }

    public bool IsValid => EndMax > StartMin;
    public bool Evaluate(float position) => position >= StartMin && position <= EndMax;
}

public struct ZonesContainer
{
    public ZoneInfo Graze;
    public ZoneInfo Hit;
    public ZoneInfo Critical;

    public ZonesContainer(ZoneInfo graze, ZoneInfo hit, ZoneInfo critical)
    {
        Graze = graze;
        Hit = hit;
        Critical = critical;
    }
}

public static class QTEMath
{
    public static QTEGrade EvaluateResult(ZonesContainer zones, float position)
    {
        // 우선순위: Critical -> Hit -> Graze
        if (zones.Critical.IsValid && zones.Critical.Evaluate(position)) return QTEGrade.Critical;
        if (zones.Hit.IsValid && zones.Hit.Evaluate(position)) return QTEGrade.Hit;
        if (zones.Graze.IsValid && zones.Graze.Evaluate(position)) return QTEGrade.Graze;

        return QTEGrade.Miss;
    }

    public static Color GetZoneColor(QTEGrade grade)
    {
        switch (grade)
        {
            case QTEGrade.Miss: return Color.gray;
            case QTEGrade.Graze: return Color.green;
            case QTEGrade.Hit: return Color.yellow;
            case QTEGrade.Critical: return Color.red;
            default: return Color.white;
        }
    }
}