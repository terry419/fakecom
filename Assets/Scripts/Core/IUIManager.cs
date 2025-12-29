using System;

/// <summary>
/// UI 관련 상호작용을 위한 인터페이스.
/// 실제 구현은 UIManager 클래스가 담당합니다.
/// </summary>
public interface IUIManager
{
    /// <summary>
    /// 전투 시작 버튼 클릭 시 발생하는 이벤트
    /// </summary>
    event Action OnStartButtonClick;

    /// <summary>
    /// 전투 시작 버튼을 활성화하고 화면에 표시합니다.
    /// </summary>
    void ShowStartButton();

    /// <summary>
    /// 전투 시작 버튼을 숨깁니다.
    /// </summary>
    void HideStartButton();
}
