using UnityEngine;

[CreateAssetMenu(fileName = "NewCurveData", menuName = "Data/Weapon/CurveData")]
public class CurveDataSO : ScriptableObject
{
    public string CurveID;

    [Tooltip("X축: 거리(Tile), Y축: 효율(0.0 ~ 1.0)")]
    public AnimationCurve Curve;
}