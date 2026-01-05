using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    // [설정] 초당 감소하는 TS 양 (기존 스크립트 값: 5f)
    [SerializeField] private float tsDecrementPerSecond = 5f;

    // [데이터] 전체 유닛 관리
    private List<UnitStatus> _allUnits = new List<UnitStatus>(); // Unit 클래스 대신 UnitStatus 사용 (기존 스크립트 참조)

    // [상태] 현재 턴 진행 중 여부
    public bool IsTurnActive { get; private set; } = false;
    public UnitStatus CurrentTurnUnit { get; private set; }

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<TurnManager>(this);
        _allUnits.Clear();
    }

    // 유닛 등록 (UnitStatus 컴포넌트 사용)
    public void RegisterUnit(UnitStatus unit)
    {
        if (!_allUnits.Contains(unit))
        {
            _allUnits.Add(unit);

            // 초기 TS 설정: 민첩성이 높을수록 0에 가깝게 시작 (바로 턴 잡기 유리)
            // 기존 코드: Random.Range(20f, 40f) / unit.Agility [cite: 690]
            float agility = unit.Agility > 0 ? unit.Agility : 10f;
            unit.CurrentTS = Random.Range(20f, 40f) / agility;
        }
    }

    public void UnregisterUnit(UnitStatus unit)
    {
        if (_allUnits.Contains(unit)) _allUnits.Remove(unit);
    }

    // [핵심] 실시간 TS 차감 로직 (User 요청 복원)
    private void Update()
    {
        // 턴이 진행 중(누군가 행동 중)이면 시간 멈춤
        if (IsTurnActive) return;
        if (_allUnits.Count == 0) return;

        // 1. 감소량 계산
        float decrement = tsDecrementPerSecond * Time.deltaTime;

        // 2. 모든 유닛 TS 차감
        // 리스트를 복사하거나 역순으로 돌 필요는 없으나, 
        // 0 도달 체크를 위해 임시 리스트를 쓸 수도 있음. 여기선 바로 체크.
        foreach (var unit in _allUnits)
        {
            if (unit.IsDead) continue; // 사망 유닛 제외

            if (unit.CurrentTS > 0)
            {
                unit.CurrentTS -= decrement;
            }

            // 3. 턴 획득 체크
            if (unit.CurrentTS <= 0)
            {
                unit.CurrentTS = 0;
                StartTurn(unit);
                break; // 한 프레임에 한 명만 턴 시작
            }
        }
    }

    private void StartTurn(UnitStatus unit)
    {
        IsTurnActive = true;
        CurrentTurnUnit = unit;

        Debug.Log($"[TurnManager] 턴 시작: {unit.name}");

        // 유닛에게 턴 시작 알림
        unit.OnTurnStart();

        // TODO: UI 갱신 이벤트 호출
    }

    public void EndTurn()
    {
        if (CurrentTurnUnit == null) return;

        // 행동에 따른 페널티(다음 대기시간) 계산
        // UnitStatus.CalculateNextTurnPenalty() 활용 [cite: 833]
        float penalty = CurrentTurnUnit.CalculateNextTurnPenalty();
        CurrentTurnUnit.CurrentTS += penalty; // 0에서 페널티만큼 증가 -> 다시 대기

        Debug.Log($"[TurnManager] 턴 종료: {CurrentTurnUnit.name}, 다음 TS: {CurrentTurnUnit.CurrentTS}");

        CurrentTurnUnit = null;
        IsTurnActive = false; // 다시 Update 루프 가동
    }

    // [GDD 11.2] 피격 페널티 적용 (외부 호출용)
    public void ApplyHitPenalty(UnitStatus target, bool isCritical)
    {
        // GDD: FinalTSPenalty의 10%/20% 적용
        // UnitStatus.TakeDamage 내부에서 호출될 수도 있지만, 
        // TurnManager가 주도권을 가지려면 여기서 처리.

        // UnitStatus에 저장된 LastFinalPenalty 사용 [cite: 820]
        float basePenalty = target.LastFinalPenalty;
        float ratio = isCritical ? 0.2f : 0.1f; // 임시 비율
        float addedDelay = basePenalty * ratio;

        target.CurrentTS += addedDelay; // 대기 시간 증가 (턴 밀림)

        Debug.Log($"[TurnManager] {target.name} 피격! TS +{addedDelay} 증가");
    }
}