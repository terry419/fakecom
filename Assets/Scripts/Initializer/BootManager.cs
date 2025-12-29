using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text; // StringBuilder 사용

public class BootManager : MonoBehaviour
{
    public static event Action<bool> OnBootComplete;

    // 성공 내역을 쌓을 변수
    private StringBuilder _bootLog = new StringBuilder();

    private async void Start()
    {
        await BootAsync();
    }

    public async UniTask<bool> BootAsync()
    {
        // 로그 초기화
        _bootLog.Clear();
        _bootLog.AppendLine("[Boot Sequence Log]");

        try
        {
            // 1. Global (AppBootstrapper 내부에서도 별도 로그를 찍지만, 여기도 한 줄 추가)
            await AppBootstrapper.EnsureGlobalSystems();
            _bootLog.AppendLine("1. Global Systems Check OK");

            // 2. Scene Managers
            // 필수 매니저들
            await InitMan<MapManager>();
            await InitMan<TilemapGenerator>();
            await InitMan<SessionManager>();

            // 선택적 매니저들
            await InitManOptional<CameraController>();
            await InitManOptional<TurnManager>();
            await InitManOptional<CombatManager>();
            await InitManOptional<PathVisualizer>();
            await InitManOptional<PlayerInputCoordinator>();
            await InitManOptional<TargetUIManager>();
            await InitManOptional<QTEManager>();
            await InitManOptional<DamageTextManager>();

            // 전부 성공 시
            _bootLog.AppendLine("<color=green>ALL SYSTEMS READY.</color>");
            Debug.Log(_bootLog.ToString()); // 최종 성공 로그 한 번에 출력

            OnBootComplete?.Invoke(true);
            return true;
        }
        catch (Exception ex)
        {
            // 실패 시: 지금까지 쌓인 성공 로그 + 에러 메시지 출력
            Debug.LogError($"<color=red>[BOOT FAILED]</color>\n" +
                           $"{_bootLog}\n" + // 어디까지 성공했는지 확인 가능
                           $"--------------------------------\n" +
                           $"<b>[Error Cause]:</b> {ex.Message}");

            OnBootComplete?.Invoke(false);
            return false;
        }
    }

    private async UniTask InitMan<T>() where T : IInitializable
    {
        if (!ServiceLocator.TryGet<T>(out var manager))
        {
            throw new Exception($"[Missing] 필수 매니저 '{typeof(T).Name}' 없음.");
        }

        await manager.Initialize(new InitializationContext());

        // [성공 로그 Append]
        _bootLog.AppendLine($"- [Scene] {typeof(T).Name} OK");
    }

    private async UniTask InitManOptional<T>() where T : IInitializable
    {
        if (ServiceLocator.TryGet<T>(out var manager))
        {
            await manager.Initialize(new InitializationContext());

            // [성공 로그 Append]
            _bootLog.AppendLine($"- [Scene] {typeof(T).Name} (Opt) OK");
        }
        else
        {
            // 없어도 에러 아님, 로그에만 (Skip) 남김
            // _bootLog.AppendLine($"- [Scene] {typeof(T).Name} Skipped (Not Found)");
        }
    }
}