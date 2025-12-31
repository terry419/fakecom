using UnityEngine;

public class MapInteractionTester : MonoBehaviour
{
    // 디버그용 광선 색상
    private void OnDrawGizmos() { /* 필요 시 구현 */ }

    void Update()
    {
        // 마우스 포인터가 가리키는 타일 찾기
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // StructureObj(물리 객체) 혹은 타일 바닥을 맞췄는지 확인
        // (레이어 설정이 안 되어있다면 Default 레이어도 체크하도록)
        if (!Physics.Raycast(ray, out RaycastHit hit, 100f)) return;

        // 1. [I]nspect: 타일 정보 조회 (이동 가능 여부 및 범인 색출)
        if (Input.GetKeyDown(KeyCode.I))
        {
            InspectTile(hit.point);
        }

        // 2. [K]ill: 구조물 파괴 테스트 (SSOT 동기화 확인)
        if (Input.GetKeyDown(KeyCode.K))
        {
            DamageStructure(hit.collider.gameObject);
        }
    }

    private void InspectTile(Vector3 hitPoint)
    {
        GridCoords coords = GridUtils.WorldToGrid(hitPoint);
        var mapMgr = ServiceLocator.Get<MapManager>();
        Tile tile = mapMgr.GetTile(coords);

        if (tile == null)
        {
            Debug.LogError($"[Inspector] Tile is NULL at {coords}");
            return;
        }

        Debug.Log($"<color=cyan>--- Tile Inspector [{coords}] ---</color>");
        Debug.Log($"Floor: {tile.FloorID} | <b>IsWalkable: {tile.IsWalkable}</b>");

        // 1. 기둥/점유자 확인
        // (주의: Tile 클래스의 _occupants는 private이므로, IsWalkable이 false라면 원인을 추론)
        if (!tile.IsWalkable)
        {
            Debug.Log($"<color=red>[Blocked]</color> This tile is blocked.");
        }

        // 2. 4방향 벽 상태 확인 (SSOT 검증용)
        for (int i = 0; i < 4; i++)
        {
            Direction dir = (Direction)i;
            RuntimeEdge edge = tile.GetEdge(dir);
            if (edge != null && edge.Type != EdgeType.Open)
            {
                string status = edge.IsBroken ? "BROKEN" : $"HP {edge.CurrentHP}/{edge.MaxHP}";
                Debug.Log($"Edge [{dir}]: {edge.Type} | {status} | Blocking: {edge.IsBlocking}");
            }
        }
    }

    private void DamageStructure(GameObject hitObj)
    {
        // 맞은 놈이 구조물(StructureObj)인지 확인
        var structure = hitObj.GetComponent<StructureObj>();
        if (structure != null)
        {
            Debug.Log($"<color=orange>[Attack]</color> Hitting {structure.name}...");
            structure.TakeDamage(50); // 데미지 50

            // 때린 직후, 연결된 타일들의 상태를 로그로 확인하기 위해
            // 약간의 지연 후(혹은 즉시) Inspect를 호출해보면 좋음
            InspectTile(hitObj.transform.position);
        }
        else
        {
            Debug.LogWarning("Not a structure (No StructureObj component).");
        }
    }
}