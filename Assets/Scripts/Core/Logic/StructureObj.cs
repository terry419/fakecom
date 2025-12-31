using UnityEngine;

// [Refactoring Phase 3] 벽/기둥 프리팹에 부착되어 물리적 파괴 이벤트를 담당
public class StructureObj : MonoBehaviour
{
    [Header("Identity")]
    public GridCoords Coords;
    public Direction Direction; // 벽일 경우 방향
    public bool IsPillar;       // 기둥 여부

    [Header("State")]
    public float MaxHP;
    public float CurrentHP;

    public void Initialize(GridCoords coords, Direction dir, float hp, bool isPillar)
    {
        Coords = coords;
        Direction = dir;
        CurrentHP = hp;
        MaxHP = hp; // 초기 생성 시 MaxHP는 현재 HP 혹은 데이터의 MaxHP로 설정
        IsPillar = isPillar;
    }

    // 테스트 툴(MapInteractionTester)이나 투사체 충돌 시 호출됨
    public void TakeDamage(float amount)
    {
        if (CurrentHP <= 0) return;

        CurrentHP -= amount;
        // Debug.Log($"Structure Damaged: {CurrentHP}/{MaxHP}");

        if (CurrentHP <= 0)
        {
            Break();
        }
    }

    private void Break()
    {
        Debug.Log($"Structure Destroyed at {Coords}");

        // 1. EnvironmentManager에게 파괴 사실 통보 (로직 동기화)
        var envManager = ServiceLocator.Get<EnvironmentManager>();
        if (envManager != null)
        {
            if (IsPillar)
            {
                // 기둥 파괴 메서드 (EnvironmentManager에 추가 필요)
                // envManager.DamagePillarAt(Coords, 9999); 
            }
            else
            {
                envManager.DamageWallAt(Coords, Direction, 9999);
            }
        }

        // 2. 자기 자신 삭제 (또는 잔해 모델로 교체)
        Destroy(gameObject);
    }
}