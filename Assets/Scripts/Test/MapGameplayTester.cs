using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// [최종 진단 및 테스트 버전]
/// 각 입력에 따라 명확한 로그를 콘솔에 출력하는 테스트 도구.
/// - 좌클릭: Raycast 진단 정보 출력
/// - 우클릭: 타일 이동 가능성 테스트
/// - 'D' 키: 구조물 파괴 시뮬레이션
/// </summary>
public class MapGameplayTester : MonoBehaviour
{
    // --- Lazy-loading Properties ---
    private MapManager _mapManager;
    private MapManager MapManager => _mapManager ?? (_mapManager = ServiceLocator.Get<MapManager>());

    private Camera _mainCamera;
    private Camera MainCamera => _mainCamera ?? (_mainCamera = Camera.main);
    
    // --- Unity Lifecycle ---

    private void Awake()
    {
        Debug.Log("<color=cyan>[Tester] Tester is active. Left-click for Raycast-Info, Right-click for Walk-Test, 'D' for Destroy-Test.</color>");
    }

    private void Update()
    {
        if (MapManager == null || MainCamera == null) return;
        
        HandleInputs();
    }

    // --- Core Logic ---

    private void HandleInputs()
    {
        // 좌클릭: 진단 정보
        if (Input.GetMouseButtonDown(0))
        {
            PerformRaycastDiagnosis();
        }
        // 우클릭: 이동 가능성 테스트
        if (Input.GetMouseButtonDown(1)) 
        {
            TestWalkability();
        }
        // 'D' 키: 파괴 테스트
        if (Input.GetKeyDown(KeyCode.D))
        {
            TestDestruction();
        }
    }

    /// <summary>
    /// 마우스 위치의 타일을 찾는 가장 견고한 방법을 사용합니다.
    /// 1. Raycast로 충돌한 오브젝트의 이름에서 좌표를 파싱합니다. (가장 정확)
    /// 2. 이름 파싱에 실패하면, 충돌 지점의 월드 좌표를 반올림하여 타일을 찾습니다. (차선책)
    /// </summary>
    private bool TryGetTileUnderCursor(out Tile tile)
    {
        tile = null;
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);

        bool originalQueriesHitTriggers = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = true;

        bool didHit = Physics.Raycast(ray, out RaycastHit hit, 1000f);
        Physics.queriesHitTriggers = originalQueriesHitTriggers;

        if (!didHit) return false;

        // 1. 이름에서 좌표 파싱 시도 (예: "Pillar_(18, 6, 0)")
        Match match = Regex.Match(hit.collider.gameObject.name, @"\((\d+),\s*(\d+),\s*(\d+)\)");
        if (match.Success)
        {
            int x = int.Parse(match.Groups[1].Value);
            int y = int.Parse(match.Groups[2].Value);
            int z = int.Parse(match.Groups[3].Value);
            var coords = new GridCoords(x, y, z);
            tile = MapManager.GetTile(coords);
            if (tile != null) return true;
        }

        // 2. 이름 파싱 실패 시, 월드 좌표로 계산
        var worldPos = hit.point;
        // 참고: Y(높이) 레벨을 0으로 가정합니다.
        var fallbackCoords = new GridCoords(Mathf.RoundToInt(worldPos.x), 0, Mathf.RoundToInt(worldPos.z));
        tile = MapManager.GetTile(fallbackCoords);
        return tile != null;
    }
    
    // --- Test Methods ---

    private void PerformRaycastDiagnosis()
    {
        Ray ray = MainCamera.ScreenPointToRay(Input.mousePosition);
        bool originalQueriesHitTriggers = Physics.queriesHitTriggers;
        Physics.queriesHitTriggers = true;
        
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            GameObject hitObject = hit.collider.gameObject;
            Debug.Log($"<color=white>===== [Left-Click] Raycast Diagnosis =====</color>\n" +
                      $"<b>Name:</b> {hitObject.name}\n" +
                      $"<b>Tag:</b> {hitObject.tag}\n" +
                      $"<b>Layer:</b> {LayerMask.LayerToName(hitObject.layer)}\n" +
                      $"<b>Hit Point (World):</b> {hit.point}\n" +
                      "=====================================");
        }
        else
        {
            Debug.LogWarning("[Left-Click] Raycast hit NOTHING.");
        }
        Physics.queriesHitTriggers = originalQueriesHitTriggers;
    }

    private void TestWalkability()
    {
        if (TryGetTileUnderCursor(out Tile tile))
        {
            bool isWalkable = tile.IsWalkable;
            bool hasPillar = tile.InitialPillarID != PillarType.None;
            string pillarInfo = hasPillar ? "(Pillar exists)" : "";

            if (isWalkable)
            {
                Debug.Log($"<color=green>[Right-Click] Walk Test: SUCCESS. Tile {tile.Coordinate} is WALKABLE.</color>");
            }
            else
            {
                Debug.Log($"<color=orange>[Right-Click] Walk Test: SUCCESS. Tile {tile.Coordinate} is UNWALKABLE {pillarInfo}.</color>");
            }
        }
        else
        {
            Debug.LogWarning("[Right-Click] Walk Test: No valid tile under cursor.");
        }
    }

    private void TestDestruction()
    {
        if (TryGetTileUnderCursor(out Tile tile))
        {
            Debug.Log($"<color=cyan>[D-Key] Destroy Test: Attempting to destroy structures at {tile.Coordinate}.</color>");
            Debug.LogWarning($"[Tester] 파괴 테스트: 현재 {tile.Coordinate}의 구조물에 데미지를 주는 Public API가 필요합니다 (예: EnvironmentManager.DamageStructureAt(coords, damage)).");
        }
        else
        {
            Debug.LogWarning("[D-Key] Destroy Test: No valid tile to destroy at cursor.");
        }
    }
}