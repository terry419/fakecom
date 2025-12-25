// 파일명: SessionManager.cs
using Cysharp.Threading.Tasks; // GDD 6.1 UniTask 사용
using System;
using System.Collections.Generic;
using UnityEngine;

public class SessionManager : MonoBehaviour, IInitializable
{
    public event Action<SessionState, SessionState> OnStateChanged;
    public SessionState CurrentState { get; private set; } = SessionState.None;

    // GDD 6.3: 전투 중 획득한 아이템 임시 저장고
    private List<object> _pendingLoot = new List<object>();

    public void Initialize()
    {
        // 씬 로드 시 SceneInitializer에 의해 호출됨
        _pendingLoot.Clear();
        ChangeState(SessionState.Boot);
    }

    public void ChangeState(SessionState newState)
    {
        if (CurrentState == newState) return;

        // [안전장치] 저장 중에는 상태 변경을 제한함
        if (CurrentState == SessionState.Saving && newState != SessionState.Error) return;

        SessionState oldState = CurrentState;
        CurrentState = newState;

        HandleStateEntry(newState).Forget();
        OnStateChanged?.Invoke(oldState, newState);
    }

    private async UniTaskVoid HandleStateEntry(SessionState state)
    {
        switch (state)
        {
            case SessionState.Boot:
                await ProcessBooting(); // 자동 전환 로직
                break;

            case SessionState.Setup:
                ResumeGameTime(); // 시간 복구
                // MapManager.Generate() 호출 로직 위치
                ChangeState(SessionState.TurnWaiting);
                break;

            case SessionState.TurnWaiting:
                ResumeGameTime(); // 시간 복구
                // TurnManager.DetermineNextUnit() 호출 (GDD 11.3)
                break;

            case SessionState.SystemOption:
                PauseGameTime(); // 시간 정지
                break;

            case SessionState.Resolution:
                PauseGameTime();
                // GDD 6.3: Pending Loot를 세이브 데이터로 전송
                break;

            case SessionState.Retry:
                // 재귀 호출을 피하기 위해 다음 프레임에 Setup으로 전환
                await UniTask.Yield();
                ChangeState(SessionState.Setup);
                break;
        }
    }

    // [개선] 시간 제어 로직 통합
    private void PauseGameTime() => Time.timeScale = 0f;
    private void ResumeGameTime() => Time.timeScale = 1f;

    private async UniTask ProcessBooting()
    {
        // GDD 8.0: 데이터 매니저를 통한 SO 로드 확인
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        ChangeState(SessionState.Setup);
    }

    public void AddLoot(object item) => _pendingLoot.Add(item);
}