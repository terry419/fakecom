using UnityEngine;
using System.Collections.Generic;
using System.Text;
using System.Linq;

/// <summary>
/// [Phase 1 Final Fix] 전투 시스템 데이터 타입 및 파이프라인 정의
/// - GetData 안전성 강화 (InvalidCastException 방지)
/// - UI 예측용 Min/Max 추적 추가
/// </summary>

// 1. 공격 결과 타입
public enum AttackResultType { Miss, Graze, Hit, Critical }

// 2. 실행 순서 (Modifier Priority)
public static class ModifierPriority
{
    // 명중률 (0~999)
    public const int HC_Base = 0;
    public const int HC_Environment = 200;
    public const int HC_Final = 900;

    // 데미지 (1000~1999)
    public const int DMG_Base = 1000;       // Step 1: 기본 피해
    public const int DMG_Efficiency = 1100; // Step 3: 공방 효율
    public const int DMG_Crit = 1200;       // Step 4: 치명타/배율
    public const int DMG_FinalClamp = 1999; // Step 5: 최종 처리
}

// 3. 컨텍스트 기본 (데이터 버스)
public abstract class CombatContextBase
{
    public Unit Attacker { get; }
    public Unit Target { get; }
    public WeaponDataSO Weapon { get; }
    public AmmoDataSO Ammo { get; }
    public bool IsPrediction { get; } // UI 예측 모드 여부

    // [Fix] 범위 추적 (UI 표시용)
    public float CurrentMinDamage { get; set; }
    public float CurrentMaxDamage { get; set; }

    private Dictionary<string, object> _data = new Dictionary<string, object>();
    private StringBuilder _logBuilder = new StringBuilder(); // [Fix] 로그 기능 복구

    protected CombatContextBase(Unit attacker, Unit target, WeaponDataSO weapon, AmmoDataSO ammo, bool isPrediction)
    {
        Attacker = attacker;
        Target = target;
        Weapon = weapon;
        IsPrediction = isPrediction;
        // [Safety] 탄약 없을 시 무기 기본값 사용
        Ammo = ammo ?? (weapon != null ? weapon.DefaultAmmo : null);
    }

    public void SetData<T>(string key, T value) => _data[key] = value;

    // [Critical Fix] 잘못된 타입 요청 시 크래시(Exception) 대신 기본값 반환
    public T GetData<T>(string key, T defaultValue = default)
    {
        if (_data.TryGetValue(key, out var val) && val is T castedVal)
        {
            return castedVal;
        }
        return defaultValue;
    }

    public void AddLog(string log)
    {
        if (!IsPrediction) _logBuilder.AppendLine(log);
    }

    public string GetLogSummary() => _logBuilder.ToString();

    protected void CopyDataFrom(CombatContextBase other)
    {
        if (other._data != null) _data = new Dictionary<string, object>(other._data);
    }
}

// [Fix] 누락되었던 HitChanceContext 클래스 복구
public class HitChanceContext : CombatContextBase
{
    public float BaseHitChance { get; set; }
    public HitChanceContext(Unit attacker, Unit target, WeaponDataSO weapon, AmmoDataSO ammo, bool isPrediction)
        : base(attacker, target, weapon, ammo, isPrediction) { }
}

// 데미지 계산 컨텍스트
public class DamageContext : CombatContextBase
{
    public AttackResultType Result { get; set; }

    public DamageContext(HitChanceContext prevContext, AttackResultType result)
        : base(prevContext.Attacker, prevContext.Target, prevContext.Weapon, prevContext.Ammo, prevContext.IsPrediction)
    {
        Result = result;
        CopyDataFrom(prevContext);
    }
}

// 모디파이어 인터페이스
public interface IDamageModifier
{
    int Priority { get; }
    bool CanApply(DamageContext context);
    float Apply(float currentDamage, DamageContext context);
}