using UnityEngine;
using System;

// [Pillar Promotion] 기둥을 단순 데이터가 아닌 '객체'로 취급
public class PillarInfo : ITileOccupant
{
    public OccupantType Type => OccupantType.Obstacle;

    // 기둥은 파괴되기 전까지 이동을 막음
    public bool IsBlockingMovement => _currentHP > 0;
    public bool IsCover => true;

    public event Action<bool> OnBlockingChanged;
    public event Action<bool> OnCoverChanged;

    public PillarType PillarID { get; private set; }
    private float _maxHP;
    private float _currentHP;

    public PillarInfo(PillarType id, float maxHP, float currentHP)
    {
        PillarID = id;
        _maxHP = maxHP;
        _currentHP = currentHP;
    }

    public void TakeDamage(float amount)
    {
        if (_currentHP <= 0) return;

        _currentHP -= amount;

        // 파괴 판정
        if (_currentHP <= 0)
        {
            _currentHP = 0;
            // 파괴되었으므로 이동 방해 해제 알림 -> Tile이 듣고 길을 틔움
            OnBlockingChanged?.Invoke(false);
            OnCoverChanged?.Invoke(false);
        }
    }

    public void OnAddedToTile(Tile tile) { }
    public void OnRemovedFromTile(Tile tile) { }
}