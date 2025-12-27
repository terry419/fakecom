
/// <summary>
/// 무기의 작동 방식 및 애니메이션 그룹을 정의합니다.
/// </summary>
public enum WeaponType
{
    None = 0,
    Rifle,      // 돌격소총 (Hold & Release)
    Sniper,     // 저격총 (Focus Aim)
    Shotgun     // 샷건 (Timing Hit)
                // 추후 Melee(근접) 등 추가 가능
}
