// ItemEffectType.cs
public enum ItemEffectType
{
    None,
    Ammo,           // 탄약 (공격 등급)
    Damage,         // 수류탄 (피해)
    ZoneDamage,     // 화염병 (장판 피해)
    Scan,           // 스캔 (시야 확보)
    Heal,           // 체력 회복
    BuffStat,       // 스탯 강화 (스팀팩)
    CureStatus,     // 상태이상 해제 (붕대, 안정제)
    GrantImmunity   // 면역 부여 (예방주사)
}