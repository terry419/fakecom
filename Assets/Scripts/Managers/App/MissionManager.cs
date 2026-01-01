using UnityEngine;
using Cysharp.Threading.Tasks;

public class MissionManager : MonoBehaviour, IInitializable
{
    // 현재 선택된 미션 정보
    public MapEntry? SelectedMission { get; private set; }

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Global);
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<MissionManager>(ManagerScope.Global);
    }

    // UI에서 호출
    public bool TrySetMission(MapEntry entry, out string error)
    {
        if (!entry.Validate(out error)) return false;
        SelectedMission = entry;
        return true;
    }

    public async UniTask Initialize(InitializationContext context)
    {
        await UniTask.CompletedTask;
    }
}