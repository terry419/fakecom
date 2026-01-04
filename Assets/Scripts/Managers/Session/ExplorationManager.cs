using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

// [ExplorationManager] 탐험 씬을 총괄하는 컨트롤러
public class ExplorationManager : MonoBehaviour
{

    [Header("UI References")]
    // 에디터에서 Site_A ~ Site_L 까지의 버튼들을 여기에 다 넣어주세요.
    [SerializeField] private List<MissionNodeUI> _allNodes;

    private MapCatalogManager _catalogManager;
    private GameManager _gameManager;

    // [Phase 3 Mock] 아직 점수 시스템이 없으므로 임시로 1.0(Easy)으로 고정
    float targetDifficulty = 1.0f;
    private int _missionCountToSpawn = 3; // 한 번에 보여줄 미션 개수

    private void Start()
    {
        InitializeExploration().Forget();
    }

    private async UniTaskVoid InitializeExploration()
    {
        // 0. 초기화: 모든 노드 비활성화 (Gameplay 시작 시 깔끔하게)
        if (_allNodes != null)
        {
            foreach (var node in _allNodes)
            {
                if (node != null) node.gameObject.SetActive(false);
            }
        }

        // ========================================================================
        // [Fix] 에디터 테스트를 위한 Self-Boot 로직 추가
        // 씬을 단독으로 실행했을 때, Global System이 없으면 강제로 로드합니다.
        // ========================================================================
        if (!ServiceLocator.IsRegistered<GameManager>() || !ServiceLocator.IsRegistered<MapCatalogManager>())
        {
            Debug.LogWarning("[ExplorationManager] Global Managers not found. Attempting Self-Boot...");

            try
            {
                // AppBootstrapper를 통해 글로벌 시스템(GameManager, Catalog 등)을 메모리에 올림 
                await AppBootstrapper.EnsureGlobalSystems();
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ExplorationManager] Self-Boot Failed: {ex.Message}");
                return;
            }
        }

        // 1. 매니저 가져오기
        if (!ServiceLocator.TryGet(out _catalogManager) || !ServiceLocator.TryGet(out _gameManager))
        {
            Debug.LogError("[ExplorationManager] Global Managers missing! (Even after boot attempt)");
            return;
        }

        // 2. 미션 뽑기 (Phase 1 & 2 로직)
        // 현재 맵이 1개("Site_A") 뿐이라면 리스트에는 그 1개만 담겨옵니다.
        List<MissionDataSO> missions = _catalogManager.GetDistinctMissions(targetDifficulty, _missionCountToSpawn);

        if (missions == null || missions.Count == 0)
        {
            Debug.LogWarning($"[ExplorationManager] 조건(난이도 {targetDifficulty})에 맞는 미션이 없습니다.");
            return;
        }

        Debug.Log($"[ExplorationManager] {missions.Count}개의 미션을 생성했습니다.");

        // 3. 버튼 활성화 및 바인딩 (LocationID 매칭)
        foreach (var mission in missions)
        {
            string locID = mission.UI.LocationID;

            // 리스트에서 이름(또는 별도 ID)이 일치하는 노드 찾기
            var targetNode = _allNodes.Find(n => n.name == locID);

            if (targetNode != null)
            {
                targetNode.gameObject.SetActive(true); // 활성화!
                // 클릭 시 실행할 동작 정의 (세션 시작)
                targetNode.Bind(mission, targetDifficulty, OnMissionClicked);
            }
            else
            {
                Debug.LogWarning($"[ExplorationManager] 미션의 LocationID '{locID}'에 해당하는 UI 노드를 찾을 수 없습니다.");
            }
        }
    }

    // 버튼 클릭 시 호출되는 콜백
    private void OnMissionClicked(MissionDataSO mission)
    {
        Debug.Log($"[Exploration] 미션 선택됨: {mission.Definition.MissionName} -> 세션 시작 요청");

        // GameManager를 통해 실제 게임(세션) 시작
        // true: 미션 데이터의 소유권을 세션이 가짐 (DontSave 처리 등)
        _gameManager.StartSessionAsync(mission, true).Forget();
    }
}