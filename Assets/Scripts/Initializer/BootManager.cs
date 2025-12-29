using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;

public class BootManager : MonoBehaviour
{
    public static event Action<bool> OnBootComplete;

    // 에러 발생 시 경로 추적용 로그
    private StringBuilder _trackLog = new StringBuilder();

    private async void Start()
    {
        await BootAsync();
    }

    public async UniTask<bool> BootAsync()
    {
        _trackLog.Clear();
        _trackLog.AppendLine("Boot Start");

        try
        {
            // 1. Global (여기서 에러나면 AppBootstrapper가 상세 내용을 뱉음)
            await AppBootstrapper.EnsureGlobalSystems();
            _trackLog.AppendLine(" -> Global OK");

            // 2. Scene Managers
            await InitMan<MapManager>();
            await InitMan<TilemapGenerator>();
            await InitManOptional<CameraController>();
            await InitManOptional<TurnManager>();
            await InitManOptional<CombatManager>();
            await InitManOptional<PathVisualizer>();
            await InitManOptional<PlayerInputCoordinator>();
            await InitManOptional<TargetUIManager>();
            await InitManOptional<QTEManager>();
            await InitManOptional<DamageTextManager>();

            // 성공 시 딱 한 줄만 출력
            Debug.Log("<color=green>[BootManager] System Ready (All Systems Initialized)</color>");
            OnBootComplete?.Invoke(true);
            return true;
        }
        catch (Exception ex)
        {
            // 실패 시: 추적 로그 + 범인 지목
            Debug.LogError($"\n<color=red><b>[BOOT FAILED]</b></color>\n" +
                           $"Last Success: {_trackLog}\n" +
                           $"<b>Error Cause: {ex.Message}</b>");

            OnBootComplete?.Invoke(false);
            return false;
        }
    }

    // 필수 매니저 초기화 헬퍼
    private async UniTask InitMan<T>() where T : IInitializable
    {
        var name = typeof(T).Name;
        var manager = ServiceLocator.Get<T>();

        if (manager == null)
            throw new Exception($"<b>'{name}'</b>가 ServiceLocator에 없습니다! 씬에 배치되었는지 확인하세요.");

        await manager.Initialize(new InitializationContext());
        _trackLog.Append($" -> {name}");
    }

    // 선택적 매니저 초기화 헬퍼
    private async UniTask InitManOptional<T>() where T : IInitializable
    {
        var manager = ServiceLocator.Get<T>();
        if (manager != null)
        {
            await manager.Initialize(new InitializationContext());
            _trackLog.Append($" -> {typeof(T).Name}");
        }
    }
}