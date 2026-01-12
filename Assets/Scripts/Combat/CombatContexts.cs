using UnityEngine;
using System.Collections.Generic;
using System.Text;

// 데이터 키 관리 (오타 방지)
public static class CombatDataKeys
{
    public const string IsFlanked = "IsFlanked";
    public const string CoverType = "CoverType"; // None, Half, Full
    public const string HeightAdvantage = "HeightAdvantage";
}

// 컨텍스트 기본 (데이터 버스)
public abstract class CombatContextBase
{
    public Unit Attacker { get; }
    public Unit Target { get; }
    public WeaponDataSO Weapon { get; }
    public AmmoDataSO Ammo { get; }
    public bool IsPrediction { get; }

    // 거리 정보 (모디파이어 공통 사용)
    public float Distance { get; set; }

    // UI 표시용 범위 (0으로 초기화 보장)
    public float CurrentMinDamage { get; set; } = 0f;
    public float CurrentMaxDamage { get; set; } = 0f;

    private Dictionary<string, object> _data = new Dictionary<string, object>();
    private StringBuilder _logBuilder = new StringBuilder();

    protected CombatContextBase(Unit attacker, Unit target, WeaponDataSO weapon, AmmoDataSO ammo, bool isPrediction)
    {
        Attacker = attacker;
        Target = target;
        Weapon = weapon;
        Ammo = ammo;
        IsPrediction = isPrediction;
    }

    public void SetData<T>(string key, T value) => _data[key] = value;

    public T GetData<T>(string key, T defaultValue = default)
    {
        if (_data.TryGetValue(key, out var val) && val is T castedVal)
            return castedVal;
        return defaultValue;
    }

    public void AddLog(string log)
    {
        // 예측 모드 아닐 때만 로그 기록 (성능 최적화)
        if (!IsPrediction) _logBuilder.AppendLine(log);
    }

    public string GetLogSummary() => _logBuilder.ToString();

    // 데이터 복사 (HitChance -> Damage 컨텍스트 전환 시 사용)
    protected void CopyDataFrom(CombatContextBase other)
    {
        if (other._data != null) _data = new Dictionary<string, object>(other._data);
        Distance = other.Distance;
        // Min/Max Damage는 새로 계산해야 하므로 복사하지 않음 (HitChanceContext에는 없던 데이터일 수 있음)
    }
}

// 명중률 계산 컨텍스트
public class HitChanceContext : CombatContextBase
{
    public float BaseHitChance { get; set; } // 초기 명중률

    public HitChanceContext(Unit attacker, Unit target, WeaponDataSO weapon, AmmoDataSO ammo, bool isPrediction)
        : base(attacker, target, weapon, ammo, isPrediction) { }
}

// 데미지 계산 컨텍스트
public class DamageContext : CombatContextBase
{
    public AttackResultType Result { get; set; }
    public float FinalHitChance { get; private set; }

    public DamageContext(HitChanceContext prevContext, AttackResultType result, float finalHitChance)
        : base(prevContext.Attacker, prevContext.Target, prevContext.Weapon, prevContext.Ammo, prevContext.IsPrediction)
    {
        Result = result;
        FinalHitChance = finalHitChance;
        CopyDataFrom(prevContext);
    }
}