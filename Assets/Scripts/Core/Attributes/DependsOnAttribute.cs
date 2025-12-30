using System;

/// <summary>
/// IInitializable 매니저의 초기화 의존성을 명시하기 위한 Attribute.
/// 이 Attribute가 붙은 클래스는 명시된 타입의 클래스들이 먼저 초기화된 후에 초기화됩니다.
/// 예시: [DependsOn(typeof(MapManager), typeof(TurnManager))]
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class DependsOnAttribute : Attribute
{
    public Type[] Dependencies { get; }

    public DependsOnAttribute(params Type[] dependencies)
    {
        Dependencies = dependencies;
    }
}
