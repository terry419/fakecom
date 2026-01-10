using Cysharp.Threading.Tasks;

// Unit.cs가 원래 요구하던 규격에 Team(UI용)만 추가
public interface IUnitController
{
    // [UI용] 아군/적군 구분
    TeamType Team { get; }

    // [기존 호환] Unit.cs가 이걸 호출하므로 맞춰줌
    // (Initialize 역할을 겸함)
    UniTask<bool> Possess(Unit unit);

    // [기존 호환]
    UniTask Unpossess();

    // [로직] 턴 시작
    UniTask OnTurnStart();

    // [로직] 턴 종료
    void OnTurnEnd();
}