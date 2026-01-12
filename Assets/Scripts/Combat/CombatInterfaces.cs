// 명중률 보정 인터페이스 (누락분 추가)
public interface IHitChanceModifier
{
    int Priority { get; }
    bool CanApply(HitChanceContext context);
    float Apply(float currentHitChance, HitChanceContext context);
}

// 데미지 보정 인터페이스
public interface IDamageModifier
{
    int Priority { get; }
    bool CanApply(DamageContext context);
    float Apply(float currentDamage, DamageContext context);
}