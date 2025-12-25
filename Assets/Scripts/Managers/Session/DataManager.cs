using UnityEngine;

public class DataManager : MonoBehaviour
{
    public void Initialize()
    {
        // 나중에 세이브 파일 경로 확인 등의 로직 들어감
    }

    public void LoadData(int slot)
    {
        Debug.Log($"[DataManager] Loading Save Slot {slot}...");
        // TODO: JSON이나 바이너리 파일 읽어오기
    }

    public void SaveData()
    {
        Debug.Log("[DataManager] Saving Data...");
    }

    public void ClearData()
    {
        // 메모리에 들고 있던 아이템, 스탯 등등 null 처리
        Debug.Log("[DataManager] In-memory data cleared.");
    }
}