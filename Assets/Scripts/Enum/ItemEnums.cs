// 아이템(탄약, 수류탄, 소모품)이 발동하는 구체적인 효과 타입
public enum ItemEffectType
{
    None,

    // [Ammo & Grenade] 공격 계열
    Ammo,           // 탄약 (공격 등급 및 상태이상 부여용)
    Damage,         // 물리/폭발 피해 (HP 감소)
    DamageNS,       // 멘탈/시스템 피해 (NS 감소 - 해커/EMP)
    ZoneDamage,     // 장판 피해 (화염병 등)

    // [Grenade] 전술 계열
    Scan,           // 시야 확보 및 은신 감지

    // [Consumable] 보조 계열
    Heal,           // 체력 회복
    RestoreNS,      // 멘탈(NS) 회복 (안정제)
    BuffStat,       // 스탯 일시 강화 (스팀팩)
    CureStatus,     // 상태이상 즉시 해제 (붕대, 해독제)
    GrantImmunity   // 상태이상 면역 부여 (예방주사)
}

// 버프/디버프 대상 스탯
public enum StatType
{
    None,
    Mobility,       // 이동력
    Agility,        // 행동력(턴 속도)
    Aim,            // 명중률
    CritChance,     // 치명타율
    Evasion         // 회피율
}

// 상태이상 종류 (치료, 면역, 확률적 부여 대상)
public enum StatusType
{
    None,
    Bleeding,       // 출혈 (지속 HP 피해)
    Poison,         // 중독 (지속 HP 피해)
    Fracture,       // 골절 (이동력/명중률 감소)
    Burn,           // 화상 (지속 HP 피해 + 패닉 유발)
    Stun,           // 기절 (행동 불가)
    Panic,          // 공포 (통제 불능)
    SyncDebuff,     // NS 수치 저하
    SystemError     // [Hacker] 기계유닛 전용 상태이상
}