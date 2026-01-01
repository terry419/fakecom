using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;

/// <summary>
/// [Refactored] 좌표 파싱 의존성을 제거하고 수학적 계산(GridUtils)을 우선시한 개선된 테스터
/// </summary>
public class MapGameplayTester : MonoBehaviour
{
    private MapManager _mapManager;
    private MapManager MapManager => _mapManager ?? (_mapManager = ServiceLocator.Get<MapManager>());

    private Camera _mainCamera;
    private Camera MainCamera => _mainCamera ?? (_mainCamera = Camera.main);

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

    /// <summary>
    /// [수정됨] 이름 파싱보다 GridUtils 수학 계산을 우선 사용하여 타일을 찾습니다.
    /// </summary>
    private bool TryGetTileUnderCursor(out Tile tile)
    {
        tile = null;
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);

        // 1. Raycast 실행
        bool originalQueriesHitTriggers = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = true; // 트리거(타일 바닥 등)도 감지
        bool didHit = Physics.Raycast(ray, out RaycastHit hit, 1000f);
        Physics.queriesHitTriggers = originalQueriesHitTriggers;

        if (!didHit) return false;

        // 2. [변경] 충돌 지점의 월드 좌표를 그리드 좌표로 변환 (가장 정확함)
        // 이름 파싱은 비주얼 객체 이름이 변경되면 깨지므로, 수학적 계산을 우선합니다.
        GridCoords targetCoords = GridUtils.WorldToGrid(hit.point);

        // 3. 해당 좌표에 타일이 실제로 존재하는지 확인
        tile = MapManager.GetTile(targetCoords);

        // 4. (보정) 만약 null이라면, 혹시 벽면을 클릭했는지 확인하기 위해 
        //    hit.normal을 이용해 인접 타일도 체크해볼 수 있으나, 현재는 정직하게 반환.

        return tile != null;
    }

    // --- Test Methods ---

    private void PerformRaycastDiagnosis()
    {
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
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
            // 상세 진단 로직 추가
            bool isWalkable = tile.IsWalkable;
            bool hasPillarData = tile.InitialPillarID != PillarType.None;

            // 시각적(Log)으로 원인 분석
            string statusColor = isWalkable ? "green" : "red";
            string log = $"<color={statusColor}>[Walk Test] Coord: {tile.Coordinate}</color>\n" +
                         $" - <b>IsWalkable:</b> {isWalkable}\n" +
                         $" - <b>FloorID:</b> {tile.FloorID}\n" +
                         $" - <b>InitialPillarID:</b> {tile.InitialPillarID} (Data)\n";

            // [핵심] 기둥 데이터는 있는데 Walkable이 true라면? -> EnvironmentManager가 일을 안 한 것.
            if (hasPillarData && isWalkable)
            {
                log += $"<color=orange> [WARNING] 기둥 데이터({tile.InitialPillarID})가 존재하나 이동 가능합니다.\n" +
                       $"EnvironmentManager.BuildMapFeatures()가 실행되었는지, \n" +
                       $"혹은 기둥의 HP가 0인지 확인하십시오.</color>";
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
            // ... 기존 로직 유지 ...
            var envManager = ServiceLocator.Get<EnvironmentManager>();
            if (envManager != null)
            {
                // 임시 테스트용 파괴 호출
                Debug.Log($"Destruction Test at {tile.Coordinate}");
                // 실제 구현 시: envManager.DamageStructureAt(...)
            }
        }
    }
}