using UnityEngine;

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Data/Item/Consumable")]
public class ConsumableDataSO : ScriptableObject
{
    [Header("Basic Info")]
    public string ItemID;
    public string DisplayName;

    // 추후 GDD 8.4에 따른 효과(Effect) 필드 추가 예정
    // 현재는 컴파일 에러 방지용 스켈레톤입니다.
}