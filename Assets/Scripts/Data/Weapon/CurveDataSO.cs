using UnityEngine;

[CreateAssetMenu(fileName = "NewCurveData", menuName = "Data/Weapon/CurveData")]
public class CurveDataSO : ScriptableObject
{
    public string CurveID;

    [Tooltip("X축: 거리(Tile), Y축: 효율(0.0 ~ 1.0)")]
    public AnimationCurve Curve;

    /// <summary>
    /// [Phase 3] 거리(distance)에 따른 효율을 반환합니다.
    /// CombatManager의 거리 보정 계산에서 호출됩니다.
    /// </summary>
    public float Evaluate(float distance)
    {
        return Curve.Evaluate(distance);
    }
}