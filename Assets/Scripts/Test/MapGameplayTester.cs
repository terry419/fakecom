using UnityEngine;

public class MapGameplayTester : MonoBehaviour
{
    private MapManager _mapManager;
    private Camera _mainCamera;

    private void Start()
    {
        // 1. 매니저와 카메라 연결 확인
        _mapManager = ServiceLocator.Get<MapManager>();
        _mainCamera = Camera.main;

        Debug.Log($"<color=yellow>[Tester] 시작됨. MapManager: {(_mapManager != null ? "OK" : "NULL")}, Camera: {(_mainCamera != null ? "OK" : "NULL")}</color>");
    }

    private void Update()
    {
        // 2. 게임 루프 생존 확인 (스페이스바)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[Tester] 스페이스바 입력됨 - Update() 루프 정상 작동 중.");
        }

        // 우클릭 디버깅
        if (Input.GetMouseButtonDown(1))
        {
            Debug.Log("[Tester] 우클릭 감지됨 -> Raycast 시도");
            CheckTileExistence("Move Check");
        }

        // D키 디버깅
        if (Input.GetKeyDown(KeyCode.D))
        {
            Debug.Log("[Tester] D키 감지됨 -> Raycast 시도");
            CheckTileExistence("Destruction Check");
        }
    }

    private void CheckTileExistence(string actionName)
    {
        if (_mapManager == null)
        {
            Debug.LogError("[Tester] MapManager가 연결되지 않아 테스트 불가.");
            return;
        }

        GridCoords coords = GetMouseGridCoords();

        // Raycast가 빗나갔으면 coords가 (0,0,0)일 수 있음. 로그 확인 필요.

        Tile tile = _mapManager.GetTile(coords);

        if (tile == null)
        {
            Debug.LogWarning($"[{actionName}] 결과: 타일 데이터 없음 (NULL). 좌표: {coords}");
        }
        else
        {
            Debug.Log($"<color=green>[{actionName}] 성공! 타일 찾음. 좌표: {coords} / FloorID: {tile.FloorID}</color>");
        }
    }

    private GridCoords GetMouseGridCoords()
    {
        if (_mainCamera == null) return new GridCoords(0, 0, 0);

        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // [중요] 씬 뷰에서 레이가 나가는지 눈으로 확인하기 위해 빨간 선 표시
        Debug.DrawRay(ray.origin, ray.direction * 1000, Color.red, 1.0f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Debug.Log($"[Tester] Raycast 충돌함: {hit.collider.name} (위치: {hit.point})");

            int x = Mathf.RoundToInt(hit.point.x);
            int z = Mathf.RoundToInt(hit.point.z);
            return new GridCoords(x, 0, z);
        }
        else
        {
            Debug.LogError("[Tester] Raycast 실패! (허공을 클릭했거나, 맵 타일에 Collider가 없습니다)");
            return new GridCoords(0, 0, 0);
        }
    }
}