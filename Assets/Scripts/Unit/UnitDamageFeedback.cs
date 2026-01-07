using UnityEngine;

[RequireComponent(typeof(UnitHealthSystem))]
public class UnitDamageFeedback : MonoBehaviour
{
    private UnitHealthSystem _healthSystem;
    private bool _isInitialized = false;

    public void Initialize(UnitHealthSystem healthSystem)
    {
        _healthSystem = healthSystem;
        if (_healthSystem != null)
        {
            _healthSystem.OnDamageTaken += HandleDamageTaken;
            _isInitialized = true;
            Debug.Log($"[UnitDamageFeedback] '{name}' 초기화 완료. 이벤트 구독됨.");
        }
        else
        {
            Debug.LogError($"[UnitDamageFeedback] '{name}' 초기화 실패: HealthSystem이 Null입니다.");
        }
    }

    private void OnDisable()
    {
        if (_healthSystem != null) _healthSystem.OnDamageTaken -= HandleDamageTaken;
    }

    private void HandleDamageTaken(int damage, bool isMyTurn, bool isCrit, float penalty, bool isStatusEffect)
    {
        // [LOG 3] 이벤트 수신 확인
        Debug.Log($"[UnitDamageFeedback] '{name}' 이벤트 수신함! 데미지: {damage}");

        if (ServiceLocator.TryGet(out DamageTextManager textManager))
        {
            // [LOG 4] 매니저 호출 시도
            Debug.Log($"[UnitDamageFeedback] 매니저에게 텍스트 생성 요청함. 위치: {transform.position}");
            bool isMiss = (damage == 0);
            textManager.ShowDamage(transform.position, damage, isCrit, isMiss);
        }
        else
        {
            Debug.LogError("[UnitDamageFeedback] DamageTextManager를 찾을 수 없습니다! (ServiceLocator 등록 안됨?)");
        }
    }
}