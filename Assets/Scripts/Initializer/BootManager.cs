using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text; // StringBuilder 사용
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

            // 2. 공유 컨텍스트 생성 및 데이터 로드
            var context = new InitializationContext();
            
            Debug.Log("[BootManager] Attempting to load MapDataSO with label 'MapData'...");
            // 맵 데이터 로드
            // 'MapData' 라벨을 가진 모든 에셋을 리스트로 불러옵니다.
            var locations = await Addressables.LoadResourceLocationsAsync("MapData").Task;
            if (locations == null || locations.Count == 0)
            {
                throw new Exception("Failed to find any assets with label 'MapData'. Please ensure your MapDataSO assets are set as Addressable and have the 'MapData' label.");
            }
            // 일단 첫 번째 에셋을 로드합니다.
            var mapDataHandle = Addressables.LoadAssetAsync<MapDataSO>(locations[0]);
            var mapData = await mapDataHandle.Task;

            if (mapDataHandle.Status != AsyncOperationStatus.Succeeded || mapData == null)
            {
                throw new Exception("Failed to load MapDataSO from Addressables via label 'MapData'.");
            }
            
            Debug.Log($"[BootManager] Successfully loaded MapDataSO: '{mapData.DisplayName}'. Assigning to context.");
            // 로드된 맵 데이터를 컨텍스트에 할당합니다.
            context.MapData = mapData;
            _bootLog.AppendLine("2. Shared Context Created & MapData Loaded");


            // 3. Scene Managers (공유 컨텍스트 사용)
            // 필수 매니저들
            Debug.Log("[BootManager] Calling Initialize on MapManager...");
            await InitMan<MapManager>(context);
            await InitMan<TilemapGenerator>(context);
            await InitMan<SessionManager>(context);

            // 선택적 매니저들
            await InitManOptional<CameraController>(context);
            await InitManOptional<TurnManager>(context);
            await InitManOptional<CombatManager>(context);
            await InitManOptional<PathVisualizer>(context);
            await InitManOptional<PlayerInputCoordinator>(context);
            await InitManOptional<TargetUIManager>(context);
            await InitManOptional<QTEManager>(context);
            await InitManOptional<DamageTextManager>(context);

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
                           "--------------------------------\n" +
                           $"<b>[Error Cause]:</b> {ex.Message}");

            OnBootComplete?.Invoke(false);
            return false;
        }
    }

    private async UniTask InitMan<T>(InitializationContext context) where T : IInitializable
    {
        if (!ServiceLocator.TryGet<T>(out var manager))
        {
            throw new Exception($"[Missing] 필수 매니저 '{typeof(T).Name}' 없음.");
        }

        await manager.Initialize(context);

        // [성공 로그 Append]
        _bootLog.AppendLine($"- [Scene] {typeof(T).Name} OK");
    }

    private async UniTask InitManOptional<T>(InitializationContext context) where T : IInitializable
    {
        if (ServiceLocator.TryGet<T>(out var manager))
        {
            await manager.Initialize(context);

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
