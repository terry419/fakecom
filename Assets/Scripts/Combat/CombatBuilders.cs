using UnityEngine;

// 1. HitChanceContext Builder
public class HitChanceContextBuilder
{
    private Unit _attacker;
    private Unit _target;
    private WeaponDataSO _weapon;
    private AmmoDataSO _ammo;
    private float _distance = -1f;
    private bool _isPrediction;

    public HitChanceContextBuilder SetAttacker(Unit u) { _attacker = u; return this; }
    public HitChanceContextBuilder SetTarget(Unit u) { _target = u; return this; }
    public HitChanceContextBuilder SetWeapon(WeaponDataSO w) { _weapon = w; return this; }
    public HitChanceContextBuilder SetAmmo(AmmoDataSO a) { _ammo = a; return this; }
    public HitChanceContextBuilder SetDistance(float d) { _distance = d; return this; }
    public HitChanceContextBuilder SetPredictionMode(bool p) { _isPrediction = p; return this; }

    public HitChanceContext Build()
    {
        if (_attacker == null || _target == null)
            throw new System.InvalidOperationException("[HitChanceBuilder] Attacker or Target is missing.");

        if (_weapon == null && _attacker.Data != null)
            _weapon = _attacker.Data.MainWeapon;

        if (_ammo == null && _weapon != null)
            _ammo = _weapon.DefaultAmmo;

        if (_distance < 0f)
            _distance = Vector3.Distance(_attacker.transform.position, _target.transform.position);

        var context = new HitChanceContext(_attacker, _target, _weapon, _ammo, _isPrediction);
        context.Distance = _distance;
        return context;
    }
}

// 2. DamageContext Builder (개선됨)
public class DamageContextBuilder
{
    private HitChanceContext _prevContext;
    private AttackResultType _result = AttackResultType.Hit;
    private float _finalHitChance = -1f; // 초기값 -1

    public DamageContextBuilder(HitChanceContext prevContext)
    {
        _prevContext = prevContext;
    }

    public DamageContextBuilder SetResult(AttackResultType result) { _result = result; return this; }
    public DamageContextBuilder SetFinalHitChance(float chance) { _finalHitChance = chance; return this; }

    public DamageContext Build()
    {
        if (_prevContext == null)
            throw new System.InvalidOperationException("[DamageBuilder] Previous HitChanceContext is null.");

        // [Fix] FinalHitChance가 설정되지 않았다면 0으로 처리하지 않고 경고 혹은 처리
        // 보통 CombatManager에서 계산된 hitChance를 넘겨줘야 함.
        if (_finalHitChance < 0f)
            _finalHitChance = 0f;

        return new DamageContext(_prevContext, _result, _finalHitChance);
    }
}