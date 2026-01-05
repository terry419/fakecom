using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 유닛에 부착되어 지속 효과(상태이상)들을 관리하는 컴포넌트입니다.
/// </summary>
[RequireComponent(typeof(UnitStatus))]
public class StatusEffectController : MonoBehaviour
{
    private UnitStatus _owner;
    private readonly List<IDurationEffect> _activeEffects = new List<IDurationEffect>();

    private void Awake()
    {
        _owner = GetComponent<UnitStatus>();
    }

    /// <summary>
    /// 이 유닛에게 새로운 지속 효과를 추가합니다.
    /// </summary>
    /// <param name="effect">추가할 효과의 로직</param>
    /// <param name="user">효과를 시전한 유닛</param>
     public void AddEffect(IDurationEffect effect, UnitStatus user)
    {
    // TODO: 동일한 효과에 대한 중첩/갱신 정책 구현
        _activeEffects.Add(effect);
        effect.OnApply(user, _owner);
        RecalculateTotalPenalty();
    }

    /// <summary>
    /// 턴이 시작되거나 시간이 경과할 때 호출되어 모든 활성 효과를 갱신합니다.
    /// (TurnManager 등에 의해 호출되어야 합니다.)
    /// </summary>
    public void TickEffects()
    {
        // 리스트에서 아이템이 제거될 수 있으므로, 역순으로 순회하는 것이 안전합니다.
        for (int i = _activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = _activeEffects[i];
            effect.OnTick();

            if (effect.IsFinished)
            {
                effect.OnRemove();
                _activeEffects.RemoveAt(i);

                RecalculateTotalPenalty();
            }
        }
    }

    private float _currentTotalPenalty = 1.0f; // 미리 계산해서 저장해둘 변수

    public void RecalculateTotalPenalty()
    {
        float penaltyMultiplier = 1.0f;

        // 리스트를 돌며 모든 패널티를 곱합니다.
        foreach (var effect in _activeEffects)
        {
            // 남은 생존율 비율을 곱해나가는 식 (1.0 - 패널티)
            penaltyMultiplier *= (1.0f - effect.SurvivalPenalty);
        }

        _currentTotalPenalty = penaltyMultiplier;
    }


    /// <summary>
    /// 현재 모든 디버프로 인한 생존 확률 패널티 합계를 계산합니다. (0.0 ~ 1.0)
    /// </summary>
    public float GetTotalSurvivalPenalty()
    {
        return _currentTotalPenalty;
    }
}