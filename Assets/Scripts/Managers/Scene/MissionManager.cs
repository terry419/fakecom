using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

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
        else Debug.LogError("[MissionManager] MissionDataSO가 할당되지 않았습니다!");
    }

    public async UniTaskVoid StartMissionAsync()
    {
        // 1. 필수 시스템 확인
        if (!ServiceLocator.TryGet(out _unitManager) || !ServiceLocator.TryGet(out _mapManager))
        {
            Debug.LogError("[MissionManager] 필수 매니저(Unit/Map) 누락.");
            return;
        }

        if (!ServiceLocator.TryGet(out _playerController))
        {
            _playerController = FindObjectOfType<PlayerController>();
            if (_playerController == null) _playerController = new GameObject("PlayerController").AddComponent<PlayerController>();
        }
        ServiceLocator.TryGet(out _cameraController);

        // 2. 아군 스폰
        await SpawnAllies();

        // 3. 적군 스폰
        await SpawnEnemies();

        Debug.Log("--- [MissionManager] Mission Setup Complete ---");
    }

    private async UniTask SpawnAllies()
    {
        if (_testSquad == null || _testSquad.Count == 0) return;
        if (SelectedMission.PlayerSpawns == null) return;

        Unit firstUnit = null;

        foreach (var spawnData in SelectedMission.PlayerSpawns)
        {
            // [핵심] MapDataSO의 RoleTag를 이용해 좌표 검색
            if (!SelectedMission.MapData.TryGetRoleLocation(spawnData.spawnTag, out Vector2Int vPos))
            {
                Debug.LogError($"[MissionManager] 아군 스폰 태그 '{spawnData.spawnTag}'를 맵에서 찾을 수 없습니다.");
                continue;
            }

            GridCoords coords = new GridCoords(vPos.x, vPos.y, 0);

            UnitDataSO unitToSpawn = spawnData.presetUnit;
            if (unitToSpawn == null && spawnData.squadSlotIndex < _testSquad.Count)
            {
                unitToSpawn = _testSquad[spawnData.squadSlotIndex];
            }

            if (unitToSpawn == null) continue;

            Unit unit = await SpawnUnitSafe(unitToSpawn, coords);
            if (unit != null && firstUnit == null) firstUnit = unit;
        }

        if (firstUnit != null)
        {
            firstUnit.SetController(_playerController);
            if (_cameraController != null) _cameraController.SetTarget(firstUnit.transform);
            await firstUnit.OnTurnStart();
        }
    }

    private async UniTask SpawnEnemies()
    {
        if (SelectedMission.EnemySpawns == null) return;

        foreach (var enemySpawn in SelectedMission.EnemySpawns)
        {
            if (!SelectedMission.MapData.TryGetRoleLocation(enemySpawn.spawnTag, out Vector2Int vPos))
            {
                Debug.LogError($"[MissionManager] 적군 스폰 태그 '{enemySpawn.spawnTag}'를 맵에서 찾을 수 없습니다.");
                continue;
            }

            GridCoords coords = new GridCoords(vPos.x, vPos.y, 0);

            if (enemySpawn.enemyUnit != null)
            {
                Unit enemy = await SpawnUnitSafe(enemySpawn.enemyUnit, coords);
                if (enemy != null)
                {
                    // 추후 AI 설정
                    Debug.Log($"[MissionManager] 적군 '{enemy.name}' 스폰 완료 (Tag: {enemySpawn.spawnTag})");
                }
            }
        }
    }

    private async UniTask<Unit> SpawnUnitSafe(UnitDataSO data, GridCoords coords)
    {
        if (!_mapManager.HasTile(coords))
        {
            Debug.LogWarning($"[MissionManager] 스폰 좌표 {coords}에 타일이 없습니다.");
            return null;
        }
        if (_mapManager.GetTile(coords).Occupants.Count > 0)
        {
            Debug.LogWarning($"[MissionManager] 스폰 좌표 {coords}에 이미 다른 유닛이 있습니다.");
            return null;
        }

        return await _unitManager.SpawnUnitAsync(data, coords);
    }
}