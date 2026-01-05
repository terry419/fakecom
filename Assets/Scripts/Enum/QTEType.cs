public enum QTEType
{
    Survival,       // 생존 (UnitStatus.CheckSurvival)
    AttackCrit,     // 공격 크리티컬
    EnemyCrit,      // 적 공격 방어/피격
    SynchroPulse    // 오버클럭 (UnitStatus.HandleSynchroPulse)
}