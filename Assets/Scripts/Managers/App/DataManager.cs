using UnityEngine;

public class DataManager : MonoBehaviour
{
    private void Awake()
    {
        ServiceLocator.Register(this);
    }

    public void Initialize()
    {
        // ���߿� ���̺� ���� ��� Ȯ�� ���� ���� ��
    }

    public void LoadData(int slot)
    {
        Debug.Log($"[DataManager] Loading Save Slot {slot}...");
        // TODO: JSON�̳� ���̳ʸ� ���� �о����
    }

    public void SaveData()
    {
        Debug.Log("[DataManager] Saving Data...");
    }

    public void ClearData()
    {
        // �޸𸮿� ��� �ִ� ������, ���� ��� null ó��
        Debug.Log("[DataManager] In-memory data cleared.");
    }
}