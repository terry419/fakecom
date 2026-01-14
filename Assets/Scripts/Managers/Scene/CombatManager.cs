using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public enum AttackResult { Miss, Hit, Critical }

public class CombatManager : MonoBehaviour, IInitializable
{
    // ... (기존 변수들 유지) ...
    [Header("Settings Reference")]
    [SerializeField] private GlobalCombatSettingsSO _combatSettings;

    [Header("Combat Balance")]
    [Tooltip("공격 행동 수행 시 TS(Time Score) 패널티")]
    [SerializeField] private float _baseAttackCost = 20f;

    [Header("Visual Settings")]
    [SerializeField] private Projectile _projectilePrefab;
    [SerializeField] private float _projectileSpeed = 25f;
    [SerializeField] private float _shootHeightOffset = 1.5f;
    [SerializeField] private float _targetCenterOffset = 1.5f;

    [Header("Pooling")]
    [SerializeField] private int _maxPoolSize = 20;
    private Queue<Projectile> _projectilePool = new Queue<Projectile>();
    private Transform _poolRoot;

    private MapManager _mapManager;

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

    public UniTask Initialize(InitializationContext context)
    {
        _mapManager = ServiceLocator.Get<MapManager>();

        if (_combatSettings == null)
        {
            _combatSettings = Resources.Load<GlobalCombatSettingsSO>("Settings/GlobalCombatSettings");
            if (_combatSettings == null)
            {
                _combatSettings = ScriptableObject.CreateInstance<GlobalCombatSettingsSO>();
            }
        }
        return UniTask.CompletedTask;
    }

    // ========================================================================
    // 3. 전투 예측 (Prediction) [Fix: Multi-Direction Cover Check]
    // ========================================================================
    public CombatFormula.HitChanceContext GetCombatPrediction(Unit attacker, Unit target)
    {
        if (attacker == null || target == null) return default;
        if (_mapManager == null) _mapManager = ServiceLocator.Get<MapManager>();
        if (_mapManager == null) return default;

        try
        {
            var attackerData = attacker.Data;
            var targetData = target.Data;
            var weapon = attackerData?.MainWeapon;

            Vector3 attackerPos = attacker.transform.position;
            Vector3 targetPos = target.transform.position;

            float distance = Vector3.Distance(attackerPos, targetPos);
            int attackerLayer = attacker.Coordinate.y;
            int targetLayer = target.Coordinate.y;

            // -----------------------------------------------------------
            // [Fix] 엄폐 로직 개선: 단일 방향이 아닌 4방향 중 '최적의 엄폐' 선택
            // -----------------------------------------------------------
            Tile targetTile = _mapManager.GetTile(target.Coordinate);
            bool isTargetIndoor = false;

            float bestFinalCoverRating = 0f;
            float debugAngleFactor = 0f;

            if (targetTile != null)
            {
                // 4방향을 모두 돌면서, 공격을 가장 잘 막아주는 벽을 찾음
                for (int i = 0; i < 4; i++)
                {
                    Direction dir = (Direction)i;
                    RuntimeEdge edge = targetTile.GetEdge(dir);

                    // 벽이 없거나 엄폐물이 아니면 패스
                    if (edge == null || edge.Cover == CoverType.None) continue;

                    // 벽의 정면 벡터 (타일 중심 -> 바깥)
                    GridCoords dirOffset = GridUtils.GetDirectionVector(dir);
                    Vector3 coverNormal = new Vector3(dirOffset.x, 0, dirOffset.z).normalized;

                    // 이 벽 기준으로 엄폐율 계산
                    float rating = CombatFormula.CalculateCoverRating(
                        edge.Cover,
                        coverNormal,
                        attackerPos,
                        targetPos,
                        attackerLayer,
                        targetLayer,
                        isTargetIndoor,
                        _combatSettings
                    );

                    // 가장 높은 방어력을 선택 (Best Cover)
                    if (rating > bestFinalCoverRating)
                    {
                        bestFinalCoverRating = rating;

                        // 디버그용 AngleFactor 역산 (로그 표시용)
                        Vector3 attackDir = (attackerPos - targetPos).normalized;
                        debugAngleFactor = Mathf.Max(0f, Vector3.Dot(coverNormal, attackDir));
                    }
                }
            }

            // [Info] 높이 패널티 정보
            float heightFactorInfo = 1.0f;
            if (attackerLayer > targetLayer)
            {
                float deltaH = attackerLayer - targetLayer;
                heightFactorInfo = Mathf.Max(_combatSettings.MinHeightFactor,
                                             1.0f - (deltaH * _combatSettings.HeightReductionFactor));
            }

            // 5. 명중률 계산
            float baseAim = (attackerData != null) ? attackerData.Aim / 100f : 0.5f;
            float targetEvasion = (targetData != null) ? targetData.Evasion / 100f : 0f;

            float rangeBonus = 0f;
            if (weapon != null && weapon.AccuracyCurveData != null)
            {
                float efficiency = weapon.AccuracyCurveData.Evaluate(distance / GridUtils.CELL_SIZE);
                rangeBonus = (efficiency - 1.0f);
            }

            return CombatFormula.CalculateHitChance(
                baseAim,
                rangeBonus,
                targetEvasion,
                bestFinalCoverRating, // [Fix] 계산된 최적 엄폐율 적용
                debugAngleFactor,
                heightFactorInfo
            );
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CombatManager] Prediction Error: {ex.Message}");
            return default;
        }
    }

    public async UniTask<AttackResult> ExecuteAttack(Unit attacker, Unit target)
    {
        if (attacker == null || target == null) return AttackResult.Miss;

        try
        {
            var prediction = GetCombatPrediction(attacker, target);

            if (prediction.FinalHitChance <= 0f)
                Debug.Log($"[Combat] HitChance is 0%. Attack will likely miss.");

            // [Log Fix] 유닛 이름 대신 좌표도 같이 표시하여 혼동 방지
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Combat] Pred: {attacker.name}({attacker.Coordinate}) -> {target.name}({target.Coordinate}) | " +
                      $"HitChance={prediction.FinalHitChance * 100:F1}% " +
                      $"(Aim:{prediction.BaseAim * 100:F0}% + Range:{prediction.RangeBonus * 100:F1}% " +
                      $"- Eva:{prediction.TargetEvasion * 100:F0}% - Cover:{prediction.FinalCoverRating * 100:F1}%)");
#endif

            bool isHit = UnityEngine.Random.value <= prediction.FinalHitChance;
            bool isCrit = false;
            int finalDamage = 0;

            if (isHit)
            {
                int attackTier = (attacker.CurrentAmmo != null) ? attacker.CurrentAmmo.AttackTier : 1;
                float defenseTier = (target.CurrentArmor != null) ? target.CurrentArmor.DefenseTier : 0f;

                float efficiency = CombatFormula.CalculateEfficiency(attackTier, defenseTier, _combatSettings);

                int minDmg = 1, maxDmg = 2;
                float critRate = 0.1f;
                float critBonus = 1.5f;

                if (attacker.Data != null && attacker.Data.MainWeapon != null)
                {
                    var w = attacker.Data.MainWeapon;
                    minDmg = w.Damage.Min;
                    maxDmg = w.Damage.Max;
                    critRate = attacker.Data.CritChance / 100f;
                    critBonus = w.CritBonus;
                }

                int baseDmg = UnityEngine.Random.Range(minDmg, maxDmg + 1);
                float rangeMod = 1.0f + prediction.RangeBonus;

                isCrit = UnityEngine.Random.value <= critRate;
                float critMult = isCrit ? critBonus : 1.0f;

                float rawDamage = baseDmg * rangeMod * efficiency * critMult;
                finalDamage = Mathf.Max(1, Mathf.RoundToInt(rawDamage));
            }

            var animator = attacker.GetComponentInChildren<Animator>();
            if (animator != null) animator.SetTrigger("Attack");

            await HandleProjectileSequence(attacker, target, isHit);

            if (isHit)
            {
                var targetHealth = target.GetComponent<UnitHealthSystem>();
                var turnManager = ServiceLocator.Get<TurnManager>();
                bool isTargetTurn = turnManager != null && turnManager.ActiveUnit == target.Status;

                float impactPenalty = (attacker.Data?.MainWeapon != null) ? attacker.Data.MainWeapon.HitImpactPenalty : 10f;

                if (targetHealth != null)
                    targetHealth.TakeDamage(finalDamage, isTargetTurn, isCrit, impactPenalty, false);

                ShowFeedback(target.transform.position, finalDamage, isCrit, false);
                Debug.Log($"[Combat] HIT! Dmg: {finalDamage}");
            }
            else
            {
                ShowFeedback(target.transform.position, 0, false, true);
                Debug.Log($"[Combat] MISS!");
            }

            if (attacker.Status != null)
                attacker.Status.AddTurnPenalty(_baseAttackCost);

            attacker.MarkAsAttacked();

            return isHit ? (isCrit ? AttackResult.Critical : AttackResult.Hit) : AttackResult.Miss;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CombatManager] ExecuteAttack Failed: {ex.Message}");
            return AttackResult.Miss;
        }
    }

    // ... (HandleProjectileSequence, GetProjectile, ReturnProjectile, ShowFeedback 동일) ...
    private async UniTask HandleProjectileSequence(Unit attacker, Unit target, bool isHit)
    {
        if (_projectilePrefab == null) { await UniTask.Delay(300); return; }

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