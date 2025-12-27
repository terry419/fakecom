// 아이템(탄약, 수류탄, 소모품)이 발동하는 구체적인 효과 타입
public enum GrenadeDamageType
{
    Damage,         // 물리/폭발 피해 (HP 감소)
    DamageNS,       // 멘탈/시스템 피해 (NS 감소 - 해커/EMP)
    Heal
}

public enum ZoneType
{
    EMP,         
    Burn,       
    Heal,
    Scan
}


public enum ConsumableEffectType
{
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
    HP,
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
    // [부상 계열] - 치료제 필요
    Bleeding,           // 출혈 (HP 2 피해)
    HeavyBleed,         // 과다출혈 (이동 시 타일당 3 피해)
    Fracture_Arm,       // 팔 골절 (명중률 -30, 투척 거리 반감)
    Fracture_Leg,       // 다리 골절 (이동력 -50%, 회피 불가)
    // [디버프 계열] - 시간 경과/약물 해제
    Pain,               // 진통 (치명타 -10%, 방어 QTE 난이도 상승)
    // [특수 계열]
    Burn,               // 화상 (HP 3 피해)
    // [시스템 계열]
    Cutoff_Freq         // 차단 주파수 초과 (NS 회복 불가, 매 턴 NS -10)
}