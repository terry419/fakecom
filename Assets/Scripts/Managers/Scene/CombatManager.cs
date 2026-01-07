// 경로: Assets/Scripts/Managers/Scene/CombatManager.cs
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public enum AttackResult { Miss, Hit, Critical }

public class CombatManager : MonoBehaviour, IInitializable
{
    // ... (기존 밸런스/사거리 변수 유지) ...
    [Header("Combat Balance")]
    [SerializeField] private float _critMultiplier = 1.5f;
    [SerializeField] private float _baseAttackCost = 20f;
    [SerializeField] private float _hitImpactPenalty = 10f;

    [Header("Range Settings")]
    [SerializeField] private float _shortRangeDistance = 4.0f;
    [SerializeField] private float _shortRangeBonus = 0.15f;

    [Header("Visual Settings")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 25f;

    // [Fix] GDD 사양(1.5f)에 맞추고, 필요 시 조정 가능하도록 변수화
    [Tooltip("총알 발사 높이 (유닛 바닥 기준)")]
    [SerializeField] private float _shootHeightOffset = 1.5f;
    [Tooltip("총알 목표 높이 (타겟 바닥 기준)")]
    [SerializeField] private float _targetCenterOffset = 1.5f;

    // ... (기존 풀링 변수 및 로직 유지) ...
    [Header("Pooling Settings")]
    [SerializeField] private int _maxPoolSize = 20;
    private Queue<Projectile> _projectilePool = new Queue<Projectile>();
    private Transform _poolRoot;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
        _poolRoot = new GameObject("@ProjectilePool").transform;
        _poolRoot.SetParent(this.transform);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<CombatManager>())
            ServiceLocator.Unregister<CombatManager>(ManagerScope.Scene);
    }

    public UniTask Initialize(InitializationContext context) => UniTask.CompletedTask;

    public async UniTask<AttackResult> ExecuteAttack(Unit attacker, Unit target)
    {
        // 1. 검증
        if (!ValidateAttack(attacker, target)) return AttackResult.Miss;

        var attackerStatus = attacker.GetComponent<UnitStatus>();
        var targetStatus = target.GetComponent<UnitStatus>();
        var targetHealth = target.GetComponent<UnitHealthSystem>();

        // 2. 계산
        float hitChance = CalculateHitChance(attacker, target);
        bool isHit = UnityEngine.Random.value <= hitChance;
        bool isCrit = false;
        int finalDamage = 0;

        if (isHit) (finalDamage, isCrit) = CalculateDamage(attacker, target, attackerStatus, targetStatus);

        // 3. 연출
        var animator = attacker.GetComponentInChildren<Animator>();
        if (animator != null) animator.SetTrigger("Attack");

        await HandleProjectileSequence(attacker, target, isHit);

        // 4. 적용
        if (isHit)
        {
            var turnManager = ServiceLocator.Get<TurnManager>();
            bool isTargetTurn = turnManager != null && turnManager.ActiveUnit == targetStatus;

            targetHealth.TakeDamage(finalDamage, isTargetTurn, isCrit, _hitImpactPenalty, false);
            ShowFeedback(target.transform.position, finalDamage, isCrit, false);
            Debug.Log($"[Combat] HIT! Target:{target.name} Dmg:{finalDamage}");
        }
        else
        {
            ShowFeedback(target.transform.position, 0, false, true);
            Debug.Log($"[Combat] MISS! Target:{target.name}");
        }

        ApplyAttackCost(attackerStatus);
        attacker.MarkAsAttacked();

        return isHit ? (isCrit ? AttackResult.Critical : AttackResult.Hit) : AttackResult.Miss;
    }

    // ... (ValidateAttack, CalculateDamage, CalculateHitChance, ApplyAttackCost 등 기존 검증/계산 로직 동일) ...
    private bool ValidateAttack(Unit attacker, Unit target)
    {
        if (attacker == null || target == null || attacker == target) return false;
        var attackerStatus = attacker.GetComponent<UnitStatus>();
        var targetHealth = target.GetComponent<UnitHealthSystem>();
        if (attackerStatus == null || targetHealth == null) return false;

        if (attacker.HasAttacked) return false;
        if (attackerStatus.Condition == UnitCondition.Incapacitated || attackerStatus.Condition == UnitCondition.Fleeing) return false;
        if (targetHealth.IsDead) return false;
        if (attacker.Faction == target.Faction && attackerStatus.Condition != UnitCondition.FriendlyFire) return false;

        float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
        float range = (attacker.Data != null && attacker.Data.MainWeapon != null) ? attacker.Data.MainWeapon.Range : 1.5f;

        float cellSize = 1.0f;
        try { cellSize = GridUtils.CELL_SIZE; } catch { }

        if (dist > (range * cellSize) + 0.5f) return false;
        return true;
    }

    private (int damage, bool isCrit) CalculateDamage(Unit attacker, Unit target, UnitStatus aStatus, UnitStatus tStatus)
    {
        int minDmg = 1, maxDmg = 2; float critRate = 0.1f; float armor = 0f;
        if (aStatus.unitData != null)
        {
            var weapon = aStatus.unitData.MainWeapon;
            if (weapon != null) { minDmg = weapon.Damage.Min; maxDmg = weapon.Damage.Max; }
            critRate = aStatus.unitData.CritChance;
        }
        if (tStatus.unitData != null && tStatus.unitData.BodyArmor != null) armor = tStatus.unitData.BodyArmor.DefenseTier;

        int rawDmg = UnityEngine.Random.Range(minDmg, maxDmg + 1);
        bool isCrit = UnityEngine.Random.value <= critRate;
        float multiplier = isCrit ? _critMultiplier : 1.0f;
        int finalDmg = Mathf.Max(1, Mathf.RoundToInt((rawDmg * multiplier) - armor));
        return (finalDmg, isCrit);
    }

    private float CalculateHitChance(Unit attacker, Unit target)
    {
        if (attacker.Data == null || target.Data == null) return 0.5f;
        float acc = attacker.Data.Aim; float eva = target.Data.Evasion;
        float dist = Vector3.Distance(attacker.transform.position, target.transform.position);
        float rangeBonus = (dist <= _shortRangeDistance) ? _shortRangeBonus : 0f;
        return Mathf.Clamp01((acc + rangeBonus) - eva);
    }

    private void ApplyAttackCost(UnitStatus attackerStatus)
    {
        if (attackerStatus != null) attackerStatus.CurrentTS += _baseAttackCost;
    }

    // ========================================================================
    // [Fix] 투사체 높이 보정 (_shootHeightOffset 사용)
    // ========================================================================
    private async UniTask HandleProjectileSequence(Unit attacker, Unit target, bool isHit)
    {
        if (_projectilePrefab == null) { await UniTask.Delay(300); return; }

        // [Fix] 기존 1.5f 하드코딩 -> Inspector 변수 사용
        Vector3 startPos = attacker.transform.position + (Vector3.up * _shootHeightOffset) + (attacker.transform.forward * 0.5f);
        Vector3 targetPos = target.transform.position + (Vector3.up * _targetCenterOffset);

        if (!isHit)
        {
            Vector3 missOffset = (target.transform.right + Vector3.up * 0.5f).normalized * 1.5f;
            targetPos += missOffset;
        }

        Projectile projectile = GetProjectile();
        if (projectile != null)
        {
            await projectile.LaunchAsync(startPos, targetPos, _projectileSpeed);
            ReturnProjectile(projectile);
        }
    }

    // ... (GetProjectile, ReturnProjectile, ShowFeedback 기존 동일) ...
    private Projectile GetProjectile()
    {
        if (_projectilePool.Count > 0)
        {
            var p = _projectilePool.Dequeue();
            if (p != null) { p.gameObject.SetActive(true); return p; }
        }
        return Instantiate(_projectilePrefab, _poolRoot);
    }

    private void ReturnProjectile(Projectile p)
    {
        if (p == null) return;
        p.gameObject.SetActive(false);
        if (_projectilePool.Count < _maxPoolSize) _projectilePool.Enqueue(p);
        else Destroy(p.gameObject);
    }

    private void ShowFeedback(Vector3 pos, int dmg, bool isCrit, bool isMiss)
    {
        if (ServiceLocator.TryGet(out DamageTextManager tm)) tm.ShowDamage(pos, dmg, isCrit, isMiss);
    }
}