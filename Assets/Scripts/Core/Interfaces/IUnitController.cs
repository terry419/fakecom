// Assets/Scripts/Core/Interfaces/IUnitController.cs
using Cysharp.Threading.Tasks;

public interface IUnitController
{
    /// <summary>
    /// 현재 컨트롤러가 조종하고 있는 유닛입니다.
    /// </summary>
    Unit PossessedUnit { get; }

    /// <summary>
    /// 컨트롤러가 특정 유닛의 제어권을 획득합니다. (빙의)
    /// </summary>
    void Possess(Unit unit);

    /// <summary>
    /// 제어권을 포기합니다.
    /// </summary>
    void Unpossess();

    /// <summary>
    /// 턴이 시작될 때 호출됩니다. (비동기 대기 가능)
    /// </summary>
    UniTask OnTurnStart();

    /// <summary>
    /// 턴이 종료될 때 호출됩니다.
    /// </summary>
    void OnTurnEnd();
}