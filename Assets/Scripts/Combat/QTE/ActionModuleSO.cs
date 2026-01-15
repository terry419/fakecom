using UnityEngine;

[CreateAssetMenu(fileName = "ActionModule_Default", menuName = "YCOM/Combat/ActionModule")]
public class ActionModuleSO : ScriptableObject
{
    [Header("Behavior")]
    public float ScrollSpeed = 1.0f;
    public float Timeout = 5.0f;
    public bool IsPingPong = true;

    [Header("Difficulty (The 70/30 Rule)")]
    public bool IsRandomPos = true;
    [Range(0.1f, 0.9f)] public float BasePrecision = 0.7f;
    public float MinSuccessChance = 0.05f;
    public float MaxSuccessChance = 0.70f;

    // 반환 타입: ZonesContainer
    public ZonesContainer CalculateZones(float hitChance, float critChance)
    {
        float normHit = Mathf.Clamp(hitChance, 0f, 100f) / 100f;
        float normCrit = Mathf.Clamp(critChance, 0f, 100f) / 100f;

        // 1. Graze (Green)
        float successWidth = MinSuccessChance + (normHit * (MaxSuccessChance - MinSuccessChance));
        successWidth = Mathf.Clamp(successWidth, MinSuccessChance, MaxSuccessChance);

        float startPos = 0.5f - (successWidth * 0.5f);
        if (IsRandomPos)
        {
            float safeMargin = 0.05f;
            float maxStart = 1.0f - safeMargin - successWidth;
            if (safeMargin < maxStart) startPos = Random.Range(safeMargin, maxStart);
        }
        var grazeZone = new ZoneInfo(QTEGrade.Graze, startPos, startPos + successWidth);

        // 2. Hit (Yellow)
        float hitWidth = successWidth * BasePrecision;
        float hitStart = startPos + (successWidth - hitWidth) * 0.5f;
        var hitZone = new ZoneInfo(QTEGrade.Hit, hitStart, hitStart + hitWidth);

        // 3. Critical (Red)
        ZoneInfo critZone = new ZoneInfo(QTEGrade.Critical, 0, 0);
        if (normCrit > 0f)
        {
            float critWidth = hitWidth * normCrit;
            float critStart = hitStart + (hitWidth - critWidth) * 0.5f;
            critZone = new ZoneInfo(QTEGrade.Critical, critStart, critStart + critWidth);
        }

        return new ZonesContainer(grazeZone, hitZone, critZone);
    }
}