using System;
using UnityEngine;

[Serializable]
public struct StatusChanceData
{
    [Tooltip("발동시킬 상태이상 종류")]
    public StatusType Type;

    [Range(0f, 100f)]
    [Tooltip("발동 확률 (0.0 ~ 100.0%). 독립 시행으로 계산됩니다.")]
    public float Chance;

    [Tooltip("상태이상 강도 (데미지, NS감소량 등)")]
    public float Value;

    [Tooltip("상태이상 지속 턴 (0 = 즉발/영구)")]
    public int Duration;
}