using UnityEngine;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// [Refactoring Phase 1.5] BootManager와 연동하여 게임 흐름(FSM) 제어
public class SessionManager : MonoBehaviour, IInitializable
{
    // ========================================================================
    // 1. 기존 FSM 및 데이터 로직 (복구됨)
    // ========================================================================
    public event Action<SessionState, SessionState> OnStateChanged;
    public SessionState CurrentState { get; private set; } = SessionState.None;

    private List<object> _pendingLoot = new List<object>();

    // ========================================================================
    // 2. 인프라 및 초기화 (ServiceLocator & BootManager 연동)
    // ========================================================================
    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<SessionManager>(ManagerScope.Scene);
        // 이벤트 구독 해제 (메모리 누수 방지)
        BootManager.OnBootComplete -= OnSystemBootFinished;
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // SessionManager 자체의 데이터 로딩이 필요하다면 여기서 수행
        // 현재는 BootManager가 완료 신호를 줄 때까지 대기하는 구조이므로 비워둠
        _pendingLoot.Clear();
        await UniTask.CompletedTask;
    }

    private void Start()
    {
        // [핵심 변경점] 스스로 시작하지 않고, BootManager가 "모든 매니저 준비 끝!" 할 때까지 기다림
        BootManager.OnBootComplete += OnSystemBootFinished;
    }

    // BootManager가 초기화가 끝났다고 알려주면 실행되는 함수
    private void OnSystemBootFinished(bool isSuccess)
    {
        if (!isSuccess)
        {
            Debug.LogError("[SessionManager] 부팅 실패로 인해 게임을 시작할 수 없습니다. (State: Error)");
            ChangeState(SessionState.Error);
            return;
        }

        Debug.Log("[SessionManager] 시스템 부팅 완료. 게임 루프(FSM)를 가동합니다.");

        // [진입점] 초기 상태인 Setup으로 진입하여 게임 시작
        ChangeState(SessionState.Setup);
    }

    // ========================================================================
    // 3. 상태 관리 로직 (기존 코드 유지)
    // ========================================================================
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

        Debug.Log($"[SessionManager] State Change: {oldState} -> {newState}");

        HandleStateEntry(newState).Forget();
        OnStateChanged?.Invoke(oldState, newState);
    }

    private async UniTaskVoid HandleStateEntry(SessionState state)
    {
        switch (state)
        {
            case SessionState.Boot:
                // BootManager가 이미 처리했으므로 여기서는 패스하거나 대기
                break;

            case SessionState.Setup:
                ResumeGameTime();
                Debug.Log("[SessionManager] 1. 맵 생성 요청...");
                // TODO: await MapManager.Generate(); 와 같이 실제 연결

                Debug.Log("[SessionManager] 2. 유닛 배치...");
                // TODO: await UnitManager.SpawnUnits();

                Debug.Log("[SessionManager] Setup Complete. Auto-transition to TurnWaiting.");
                ChangeState(SessionState.TurnWaiting);
                break;

            case SessionState.TurnWaiting:
                ResumeGameTime();
                // TODO: TurnManager.StartTurn();
                Debug.Log("[SessionManager] 턴 대기 중...");
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

    // ========================================================================
    // 4. 유틸리티 (기존 코드 유지)
    // ========================================================================
    private void PauseGameTime()
    {
        Time.timeScale = 0f;
    }

    private void ResumeGameTime()
    {
        Time.timeScale = 1f;
    }

    public void AddLoot(object item)
    {
        _pendingLoot.Add(item);
    }
}