using UnityEngine;
using System.Collections.Generic;

public class MapGameplayTester : MonoBehaviour
{
    private MapManager _mapManager;
    private MapManager MapManager => _mapManager ?? (_mapManager = ServiceLocator.Get<MapManager>());

    private Camera _mainCamera;
    private Camera MainCamera => _mainCamera ?? (_mainCamera = Camera.main);

    // [Fix 1] 누락되었던 레이어 마스크 변수 추가 (기본값: Everything)
    [SerializeField] private LayerMask _layerMask = -1;

    private void Awake()
    {
        Debug.Log("<color=cyan>[Tester] Ready. Left: Raycast Info, Right: Walk Test, 'D': Destroy.</color>");
    }

    private void Update()
    {
        if (MapManager == null || MainCamera == null) return;
        HandleInputs();
    }

    private void HandleInputs()
    {
        if (Input.GetMouseButtonDown(0)) PerformRaycastDiagnosis();
        if (Input.GetMouseButtonDown(1)) TestWalkability();
        if (Input.GetKeyDown(KeyCode.D)) TestDestruction();
    }

    private bool TryGetTileUnderCursor(out Tile tile)
    {
        tile = null;
        var mapManager = ServiceLocator.Get<MapManager>();
        if (mapManager == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, _layerMask))
        {
            // 월드 좌표 -> 그리드 좌표 변환
            var gridCoords = mapManager.BasePosition.WorldToGridCoords(hit.point);

            // 1차 시도: 정확한 좌표 검색
            tile = mapManager.GetTile(gridCoords);

            // [Fix] 2차 시도: 만약 타일이 없고 높이(y)가 0보다 크다면, 바로 아래 칸을 검색
            // (이유: 기둥이나 벽의 윗부분을 클릭하면 y값이 1 높게 나올 수 있음)
            if (tile == null && gridCoords.y > mapManager.MinLevel)
            {
                var belowCoords = new GridCoords(gridCoords.x, gridCoords.z, gridCoords.y - 1);
                tile = mapManager.GetTile(belowCoords);

                if (tile != null)
                {
                    Debug.Log($"[Tester] Raycast 보정됨: {gridCoords} -> {belowCoords} (구조물 상단 클릭 감지)");
                }
            }
        }
        return tile != null;
    }

    // --- Test Methods ---

    private void PerformRaycastDiagnosis()
    {
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, _layerMask))
        {
            var coords = GridUtils.WorldToGrid(hit.point);
            Tile tile = MapManager.GetTile(coords);

            string logicInfo = (tile != null)
                ? $"Found Logic Tile: {tile.Coordinate} / Floor: {tile.FloorID}"
                : "<color=red>No Logic Tile Found Here</color>";

            Debug.Log($"<color=white>===== [Left-Click] Info =====</color>\n" +
                      $"<b>Hit Object:</b> {hit.collider.name}\n" +
                      $"<b>World Pos:</b> {hit.point}\n" +
                      $"<b>Calculated Grid:</b> {coords}\n" +
                      $"<b>Logic Status:</b> {logicInfo}\n" +
                      "=====================================");
        }
    }

    private void TestWalkability()
    {
        if (TryGetTileUnderCursor(out Tile tile))
        {
            bool isWalkable = tile.IsWalkable;
            bool hasPillarData = tile.InitialPillarID != PillarType.None;

            string statusColor = isWalkable ? "green" : "red";
            string log = $"<color={statusColor}>[Walk Test] Coord: {tile.Coordinate}</color>\n" +
                         $" - <b>IsWalkable:</b> {isWalkable}\n" +
                         $" - <b>FloorID:</b> {tile.FloorID}\n" +
                         $" - <b>InitialPillarID:</b> {tile.InitialPillarID} (Data)\n";

            if (hasPillarData && isWalkable)
            {
                log += $"<color=orange> [WARNING] 기둥 데이터({tile.InitialPillarID})가 존재하나 이동 가능합니다.\n" +
                       $"EnvironmentManager.BuildMapFeatures()가 실행되었는지 확인하십시오.</color>";
            }

            Debug.Log(log);
        }
        else
        {
            Debug.LogWarning("[Right-Click] Valid Tile Not Found under cursor.");
        }
    }

    private void TestDestruction()
    {
        if (TryGetTileUnderCursor(out Tile tile))
        {
            var envManager = ServiceLocator.Get<EnvironmentManager>();
            if (envManager != null)
            {
                // 기둥 파괴 테스트

            }
        }
    }
}