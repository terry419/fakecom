using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using YCOM.Mission; // [Fix] MissionDataSO 네임스페이스 참조

public class MissionManager : MonoBehaviour, IInitializable
{
    public MissionDataSO SelectedMission { get; set; }

    [Header("Debug")]
    [SerializeField] private MissionDataSO _fallbackMission;
    [SerializeField] private List<UnitDataSO> _testSquad;

    private UnitManager _unitManager;
    private MapManager _mapManager;
    private PlayerController _playerController;
    private CameraController _cameraController;

    private void Awake()
    {
        if (ServiceLocator.IsRegistered<MissionManager>()) { Destroy(gameObject); return; }
        ServiceLocator.Register(this, ManagerScope.Scene);
    }
    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<MissionManager>()) ServiceLocator.Unregister<MissionManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        Debug.Log("[MissionManager] Initialized.");
        await UniTask.CompletedTask;
    }

    private void Start()
    {
        if (SelectedMission == null && _fallbackMission != null)
        {
            Debug.Log("[MissionManager] Using Fallback Mission.");
            SelectedMission = _fallbackMission;
        }

        if (SelectedMission != null) StartMissionAsync().Forget();
        else Debug.LogError("[MissionManager] No MissionDataSO assigned!");
    }

    public async UniTaskVoid StartMissionAsync()
    {
        // 1. 필수 시스템 확보 (UnitManager, MapManager 등)
        if (!ServiceLocator.TryGet(out _unitManager) || !ServiceLocator.TryGet(out _mapManager)) return;

        // PlayerController/Camera 확보 (생략 - 기존 로직 유지)
        if (!ServiceLocator.TryGet(out _playerController))
        {
            _playerController = FindObjectOfType<PlayerController>();
            if (_playerController == null) _playerController = new GameObject("PlayerController").AddComponent<PlayerController>();
        }
        ServiceLocator.TryGet(out _cameraController);

        // 2. 맵 데이터 확인
        // MapManager는 이미 SceneInitializer에 의해 MapData를 로드하고 있어야 함.
        // 정합성 체크: Mission이 요구하는 Map과 현재 로드된 Map이 같은지?
        // (지금은 생략하지만, 추후 검증 필요)

        // 3. 아군 스폰 (태그 기반)
        await SpawnAllies();

        // 4. 적군 스폰 (태그 기반 - 추후 구현)
        // await SpawnEnemies();

        Debug.Log("--- [MissionManager] Mission Setup Complete ---");
    }

    private async UniTask SpawnAllies()
    {
        if (_testSquad == null || _testSquad.Count == 0) return;
        if (SelectedMission.PlayerSpawns == null) return;

        Unit firstUnit = null;

        // 미션 데이터에 정의된 스폰 슬롯만큼 반복
        foreach (var spawnData in SelectedMission.PlayerSpawns)
        {
            // A. 태그로 좌표 찾기 (MapDataSO에게 질문)
            if (!SelectedMission.MapData.TryGetTaggedCoords(spawnData.spawnTag, out GridCoords coords))
            {
                Debug.LogError($"[MissionManager] Spawn Tag '{spawnData.spawnTag}' not found in MapData!");
                continue;
            }

            // B. 유닛 결정 (프리셋이 있으면 프리셋, 없으면 스쿼드 슬롯)
            UnitDataSO unitToSpawn = spawnData.presetUnit;
            if (unitToSpawn == null)
            {
                if (spawnData.squadSlotIndex < _testSquad.Count)
                    unitToSpawn = _testSquad[spawnData.squadSlotIndex];
            }

            if (unitToSpawn == null) continue;

            // C. 유효성 검사 및 스폰
            if (!_mapManager.HasTile(coords) || _mapManager.GetTile(coords).Occupants.Count > 0)
            {
                Debug.LogWarning($"[MissionManager] Spawn failed at {coords} (Invalid/Occupied)");
                continue;
            }

            Unit unit = await _unitManager.SpawnUnitAsync(unitToSpawn, coords);
            if (unit != null && firstUnit == null) firstUnit = unit;
        }

        // 제어권 이양
        if (firstUnit != null)
        {
            firstUnit.SetController(_playerController);
            if (_cameraController != null) _cameraController.SetTarget(firstUnit.transform);
            await firstUnit.OnTurnStart();
        }
    }
}