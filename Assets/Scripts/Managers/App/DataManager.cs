using UnityEngine;

public class DataManager : MonoBehaviour
{
    private void Awake()
    {
        ServiceLocator.Register(this);
        Debug.Log($"[Self-Register] {nameof(DataManager)} Registered.");
    }

    private void OnDestroy()
    {
        ServiceLocator.Unregister<DataManager>();
    }

    public void Initialize()
    {
        // 데이터 로드 등 초기화
    }

    public void LoadData(int slot)
    {
        Debug.Log($"[DataManager] Loading Save Slot {slot}...");
    }

    public void SaveData()
    {
        Debug.Log("[DataManager] Saving Data...");
    }

    public void ClearData()
    {
        Debug.Log("[DataManager] In-memory data cleared.");
    }
}