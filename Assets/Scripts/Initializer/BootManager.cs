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
            // 1. Global Systems
            await AppBootstrapper.EnsureGlobalSystems();
            _bootLog.AppendLine("1. Global Systems Initialized OK");

            // 2. Scene Systems
            var sceneInitializer = FindObjectOfType<SceneInitializer>();
            if (sceneInitializer == null)
            {
                // [수정] 씬 이니셜라이저가 없으면 치명적 오류로 처리
                throw new BootstrapException("SceneInitializer not found in the current scene. Cannot proceed.");
            }
            
            await sceneInitializer.InitializeSceneAsync(_bootLog);
            _bootLog.AppendLine("2. Scene Systems Initialized OK");

            _bootLog.AppendLine("\n<color=green>ALL SYSTEMS READY.</color>");
            Debug.Log(_bootLog.ToString());
            
            OnBootComplete?.Invoke(true);
            return true;
        }
        catch (BootstrapException bex)
        {
            // ... (이하 오류 처리 로직은 동일)
            Debug.LogError(
                $"<color=red>[BOOT FAILED]</color>\n" +
                $"{_bootLog}\n" +
                $"================================\n" +
                $"<b>Error:</b> {bex.Message}\n" +
                $"<b>Inner Exception:</b> {bex.InnerException?.Message}\n" +
                $"<b>StackTrace:</b>\n{bex.StackTrace}");

            OnBootComplete?.Invoke(false);
            return false;
        }
        catch (Exception ex)
        {
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