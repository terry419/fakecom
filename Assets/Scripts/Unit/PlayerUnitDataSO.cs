using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "NewPlayerUnit", menuName = "Data/Unit/PlayerUnit")]
public class PlayerUnitDataSO : UnitDataSO
{

    [Tooltip("기본 지급 주무기")]
    public WeaponDataSO MainWeapon;

    [Tooltip("기본 지급 방어구")]
    public ArmorDataSO BodyArmor;

}