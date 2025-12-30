using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;

public class BootManager : MonoBehaviour
{
    public static event Action<bool> OnBootComplete;
    private StringBuilder _bootLog = new StringBuilder();

    private async void Start()
    {
        await BootAsync();
    }

    public async UniTask<bool> BootAsync()
    {
        _bootLog.Clear();
        _bootLog.AppendLine("[Boot Sequence Log]");

        try
        {
            // 1. Global Systems (AppBootstrapper 호출)
            // 발생하는 모든 치명적 오류는 BootstrapException으로 래핑되어 올라옵니다.
            await AppBootstrapper.EnsureGlobalSystems();
            _bootLog.AppendLine("1. Global Systems Check OK");

            // 2. Scene Context (MapData) 로드 등 나머지 초기화 로직...
            // (기존 코드 유지)

            _bootLog.AppendLine("<color=green>ALL SYSTEMS READY.</color>");
            Debug.Log(_bootLog.ToString());

            OnBootComplete?.Invoke(true);
            return true;
        }
        catch (BootstrapException bex)
        {
            // [Critical] 초기화 실패 (예상된 오류)
            Debug.LogError(
                $"<color=red>[BOOT FAILED]</color>\n" +
                $"{_bootLog}\n" +
                $"================================\n" +
                $"<b>Error:</b> {bex.Message}\n" +
                $"<b>Inner Exception:</b> {bex.InnerException?.Message}");

            OnBootComplete?.Invoke(false);
            return false;
        }
        catch (Exception ex)
        {
            // [Unexpected] 예상치 못한 런타임 오류
            Debug.LogError(
                $"<color=red>[BOOT FAILED - UNEXPECTED]</color>\n" +
                $"{_bootLog}\n" +
                $"================================\n" +
                $"<b>Unexpected Error:</b> {ex.GetType().Name}: {ex.Message}\n" +
                $"<b>StackTrace:</b>\n{ex.StackTrace}");

            OnBootComplete?.Invoke(false);
            return false;
        }
    }
}