using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class SessionManager : MonoBehaviour, IInitializable
{
    public event Action<SessionState, SessionState> OnStateChanged;
    public SessionState CurrentState { get; private set; } = SessionState.None;

    private List<object> _pendingLoot = new List<object>();

    public async UniTask Initialize(InitializationContext context)
    {
        _pendingLoot.Clear();

        // 아직 특별히 비동기 로딩할 게 없다면 바로 상태 변경
        ChangeState(SessionState.Boot);

        // 비동기 함수 규격을 맞추기 위해 완료 신호 보냄
        await UniTask.CompletedTask;
    }
    public void ChangeState(SessionState newState)
    {
        if (CurrentState == newState) return;

        if (CurrentState == SessionState.Saving && newState != SessionState.Error)
        {
            Debug.LogWarning($"[SessionManager] Cannot change state to {newState} while SAVING.");
            return;
        }

        SessionState oldState = CurrentState;
        CurrentState = newState;

        // 상태 변경 로그 (이모티콘 제거, 표준 포맷)
        Debug.Log($"[SessionManager] State Change: {oldState} -> {newState}");

        HandleStateEntry(newState).Forget();
        OnStateChanged?.Invoke(oldState, newState);
    }

    private async UniTaskVoid HandleStateEntry(SessionState state)
    {
        switch (state)
        {
            case SessionState.Boot:
                await ProcessBooting();
                break;

            case SessionState.Setup:
                ResumeGameTime();
                // MapManager.Generate() 호출 로직
                // Setup 완료 로그는 필요할 수 있음 (흐름 파악용)
                Debug.Log("[SessionManager] Setup Complete. Auto-transition to TurnWaiting.");
                ChangeState(SessionState.TurnWaiting);
                break;

            case SessionState.TurnWaiting:
                ResumeGameTime();
                // TurnManager.DetermineNextUnit()
                break;

            case SessionState.SystemOption:
                PauseGameTime();
                break;

            case SessionState.Resolution:
                PauseGameTime();
                Debug.Log($"[SessionManager] Battle Result. Pending Loot Count: {_pendingLoot.Count}");
                break;

            case SessionState.Retry:
                await UniTask.Yield();
                ChangeState(SessionState.Setup);
                break;

            case SessionState.Error:
                PauseGameTime();
                Debug.LogError("[SessionManager] Critical Session Error Occurred.");
                break;
        }
    }

    private void PauseGameTime()
    {
        Time.timeScale = 0f;
    }

    private void ResumeGameTime()
    {
        Time.timeScale = 1f;
    }

    private async UniTask ProcessBooting()
    {
        // 데이터 로드 시뮬레이션
        await UniTask.Delay(TimeSpan.FromSeconds(0.5f));
        ChangeState(SessionState.Setup);
    }

    public void AddLoot(object item)
    {
        _pendingLoot.Add(item);
    }
}