using UnityEngine;
using System;

[Serializable]
public struct MinMaxInt
{
    public int Min;
    public int Max;

    public MinMaxInt(int min, int max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Min과 Max 사이의 랜덤한 정수를 반환합니다 (Max 포함).
    /// </summary>
    public int GetRandomValue()
    {
        // Random.Range(int min, int max)는 max가 Exclusive(포함안됨)이므로 +1 해줍니다.
        if (Min > Max) return Min;
        return UnityEngine.Random.Range(Min, Max + 1);
    }
}