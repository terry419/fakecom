/// <summary>
/// 일정 기간 동안 지속되는 효과(상태이상)를 위한 인터페이스입니다.
/// 구현체는 내부에 user와 target, 그리고 남은 시간 등의 상태를 저장해야 합니다.
/// </summary>
public interface IDurationEffect
{
    /// <summary>
    /// 효과가 대상에게 처음 적용될 때 호출됩니다.
    /// 이 메소드 내에서 시전자(user)와 대상(target)을 멤버 변수로 캐싱해야 합니다.
    /// </summary>
    void OnApply(UnitStatus user, UnitStatus target);

    /// <summary>
    /// 매 턴 또는 일정 시간마다 호출됩니다.
    /// 캐싱해둔 target의 상태를 변경하는 로직을 수행합니다.
    /// </summary>
    void OnTick();

    /// <summary>
    /// 효과의 지속시간이 끝나 제거될 때 호출됩니다.
    /// </summary>
    void OnRemove();

    /// <summary>
    /// 효과의 지속시간이 끝났는지 여부를 반환합니다.
    /// </summary>
    bool IsFinished { get; }


    /// <summary>
    /// 존 확률을 깎는 비율 (0.0이면 패널티 없음, 0.2면 20% 감소)
    /// </summary>
    float SurvivalPenalty { get; }
}
