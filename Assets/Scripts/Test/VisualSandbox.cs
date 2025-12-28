using UnityEngine;
using Cysharp.Threading.Tasks;

public class VisualSandbox : MonoBehaviour
{
    private const float TileSize = 1.0f;
    private const float HalfSize = 0.5f;

    private EdgeDataManager _edgeManager;
    private TileDataManager _tileManager;

    private void Start()
    {
        WaitForInitAndRun().Forget();
    }

    private async UniTask WaitForInitAndRun()
    {
        Debug.Log("[VisualSandbox] 초기화 대기 중...");

        // 1. 매니저 로딩 대기
        await UniTask.WaitUntil(() => ServiceLocator.Get<TileDataManager>() != null);
        await UniTask.WaitUntil(() => ServiceLocator.Get<EdgeDataManager>() != null);

        _tileManager = ServiceLocator.Get<TileDataManager>();
        _edgeManager = ServiceLocator.Get<EdgeDataManager>();

        // 2. 데이터(SO) 로딩 대기
        await UniTask.WaitUntil(() => _tileManager.GetFloorData(FloorType.Concrete) != null);

        Debug.Log("========== [Visual Sandbox] 데이터 로드 확인 완료. 테스트 시작 ==========");

        Generate3x3Room();
    }

    private void Generate3x3Room()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int z = 0; z < 3; z++)
            {
                Vector3 centerPos = new Vector3(x * TileSize, 0, z * TileSize);

                // 1. 바닥 생성 (이 바닥 위에 다른 걸 올릴 겁니다)
                GameObject floorObj = SpawnFloor(FloorType.Concrete, centerPos);

                // 바닥 생성 실패시 건너뜀
                if (floorObj == null) continue;

                // 2. 테두리 벽 (바닥 높이를 기준으로 배치)
                if (z == 0) // 남쪽
                {
                    if (x == 1) SpawnEdge(EdgeDataType.WoodDoor, centerPos, Direction.South, floorObj);
                    else SpawnEdge(EdgeDataType.ConcreteWall, centerPos, Direction.South, floorObj);
                }

                if (z == 2) // 북쪽
                {
                    if (x == 1) SpawnEdge(EdgeDataType.GlassWindow, centerPos, Direction.North, floorObj);
                    else SpawnEdge(EdgeDataType.ConcreteWall, centerPos, Direction.North, floorObj);
                }

                if (x == 0) SpawnEdge(EdgeDataType.ConcreteWall, centerPos, Direction.West, floorObj);
                if (x == 2) SpawnEdge(EdgeDataType.BrickWall, centerPos, Direction.East, floorObj);

                // 3. 기둥 (바닥 높이를 기준으로 배치)
                if (x == 0 && z == 0) SpawnPillar(PillarType.Concrete, centerPos, -0.5f, -0.5f, floorObj);
                if (x == 2 && z == 0) SpawnPillar(PillarType.Concrete, centerPos, 0.5f, -0.5f, floorObj);
                if (x == 0 && z == 2) SpawnPillar(PillarType.Concrete, centerPos, -0.5f, 0.5f, floorObj);
                if (x == 2 && z == 2) SpawnPillar(PillarType.Concrete, centerPos, 0.5f, 0.5f, floorObj);
            }
        }
    }

    // --- 헬퍼 함수들 ---

    /// <summary>
    /// [수정됨] 복합 프리팹(여러 렌더러)이어도 가장 높은 윗면을 찾아 그 위에 올립니다.
    /// </summary>
    private void AlignObjectOnTop(GameObject targetObj, GameObject baseFloor)
    {
        if (targetObj == null || baseFloor == null) return;

        // 1. 바닥의 '가장 높은' 윗면 높이 구하기
        // (프리팹이 검은판+흰판으로 나뉘어 있어도 제일 위의 흰판을 찾기 위함)
        Renderer[] floorRenderers = baseFloor.GetComponentsInChildren<Renderer>();
        if (floorRenderers == null || floorRenderers.Length == 0) return;

        float floorMaxY = float.MinValue;
        foreach (var r in floorRenderers)
        {
            if (r.bounds.max.y > floorMaxY)
                floorMaxY = r.bounds.max.y;
        }

        // 2. 타겟 물체의 아랫면 높이 구하기 (얘도 혹시 모르니 전체 렌더러 중 가장 낮은 곳 기준)
        Renderer[] targetRenderers = targetObj.GetComponentsInChildren<Renderer>();
        if (targetRenderers == null || targetRenderers.Length == 0) return;

        float targetMinY = float.MaxValue;
        foreach (var r in targetRenderers)
        {
            if (r.bounds.min.y < targetMinY)
                targetMinY = r.bounds.min.y;
        }

        // 3. 현재 피벗 위치
        float currentY = targetObj.transform.position.y;

        // 4. 오차(Offset) 계산: (현재 피벗 Y - 물체의 발바닥 Y)
        float pivotToBottomOffset = currentY - targetMinY;

        // 5. 최종 위치 적용
        // 목표 Y = 바닥 최상단 + (피벗에서 발바닥까지의 거리)
        Vector3 newPos = targetObj.transform.position;
        newPos.y = floorMaxY + pivotToBottomOffset;

        targetObj.transform.position = newPos;
    }

    private GameObject SpawnFloor(FloorType type, Vector3 centerPos)
    {
        var data = _tileManager.GetFloorData(type);
        if (data != null && data.ModelPrefab != null)
        {
            // 바닥은 일단 위치에 생성 (피벗 기준)
            GameObject go = Instantiate(data.ModelPrefab, centerPos, Quaternion.identity, this.transform);

            // [선택 사항] 바닥이 지하로 안 꺼지게 하려면, 바닥 자체도 Y=0 위로 올리는 로직을 넣을 수 있음.
            // 여기서는 바닥은 그냥 둠.
            return go;
        }

        Debug.LogError($"[Sandbox] 바닥 생성 실패: {type}");
        return null;
    }

    private void SpawnEdge(EdgeDataType type, Vector3 centerPos, Direction dir, GameObject floorObj)
    {
        var data = _edgeManager.GetData(type);
        if (data != null && data.ModelPrefab != null)
        {
            Vector3 offset = Vector3.zero;
            float rotationY = 0;

            switch (dir)
            {
                case Direction.North: offset = new Vector3(0, 0, HalfSize); rotationY = 0; break;
                case Direction.East: offset = new Vector3(HalfSize, 0, 0); rotationY = 90; break;
                case Direction.South: offset = new Vector3(0, 0, -HalfSize); rotationY = 180; break;
                case Direction.West: offset = new Vector3(-HalfSize, 0, 0); rotationY = 270; break;
            }

            GameObject wall = Instantiate(data.ModelPrefab, centerPos + offset, Quaternion.Euler(0, rotationY, 0), this.transform);

            // [수정] 생성 후 높이 맞춤
            AlignObjectOnTop(wall, floorObj);
        }
    }

    private void SpawnPillar(PillarType type, Vector3 centerPos, float offsetX, float offsetZ, GameObject floorObj)
    {
        var data = _tileManager.GetPillarData(type);
        if (data != null && data.ModelPrefab != null)
        {
            Vector3 pos = centerPos + new Vector3(offsetX, 0, offsetZ);
            GameObject pillar = Instantiate(data.ModelPrefab, pos, Quaternion.identity, this.transform);

            // [수정] 생성 후 높이 맞춤
            AlignObjectOnTop(pillar, floorObj);
        }
    }
}