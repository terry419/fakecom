using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;

public class UnitSpawnTester : MonoBehaviour
{
    [Header("Test Data")]
    public UnitDataSO TestUnitData;
    public Vector2Int SpawnCoords = new Vector2Int(0, 0);

    private async void Start()
    {
        Debug.Log("--- [UnitSpawnTester] Start ---");

        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10)); // 넉넉하게 10초
        try
        {
            // 1. 매니저 등록 대기 (Awake 완료)
            await UniTask.WaitUntil(() => ServiceLocator.IsRegistered<UnitManager>(), cancellationToken: cts.Token);

            var unitManager = ServiceLocator.Get<UnitManager>();

            // 2. [핵심 수정] 실제 초기화 완료 대기 (Initialize 완료)
            Debug.Log("[UnitSpawnTester] Waiting for UnitManager Initialization...");
            await UniTask.WaitUntil(() => unitManager.IsInitialized, cancellationToken: cts.Token);

            // 3. 스폰 시작
            Debug.Log("[UnitSpawnTester] System Ready. Spawning Unit...");
            GridCoords coords = new GridCoords(SpawnCoords.x, SpawnCoords.y, 0);

            Unit unit = await unitManager.SpawnUnitAsync(TestUnitData, coords);

            // 컨트롤러 부착 및 테스트
            var controllerGO = new GameObject("PlayerController_Test");
            var controller = controllerGO.AddComponent<PlayerController>();

            unit.SetController(controller);

            Debug.Log("[UnitSpawnTester] Simulating Turn...");
            unit.ResetAP();
            await unit.OnTurnStart();

            unit.OnTurnEnd();
            Debug.Log("[UnitSpawnTester] Test Complete.");
        }
        catch (OperationCanceledException)
        {
            Debug.LogError("[UnitSpawnTester] Timeout: System initialization took too long.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[UnitSpawnTester] Failed: {ex.Message}");
        }
    }
}