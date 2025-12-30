using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

/// <summary>
/// [GDD 5.6]  ּ  (Logic Class). 
/// ̳     Ϳ   մϴ.
/// </summary>
public class Tile
{
    // ==================================================================================
    // 1. ⺻  & ̺Ʈ
    // ==================================================================================
    public GridCoords Coordinate { get; private set; }
    public FloorType FloorID { get; private set; }
    public PillarType PillarID { get; private set; }

    private EdgeInfo[] _edges = new EdgeInfo[4];

    // [̺Ʈ]  ı  ջ
    public event Action<Direction, EdgeInfo> OnEdgeDestroyed;
    public event Action<Direction, EdgeInfo> OnEdgeDamaged;

    // [̺Ʈ] ̵    (Pathfinder ſ)
    public event Action<Tile> OnWalkableStatusChanged;


    // ==================================================================================
    // 2.   (и & ȭ)
    // ==================================================================================
    private ITileOccupant _primaryUnit;
    private List<ITileOccupant> _items = new();
    private List<ITileOccupant> _obstacles = new();

    // [ȭ] б   ĳ
    private ReadOnlyCollection<ITileOccupant> _readOnlyItems;

    // [ĳ] ̵  
    private bool _cachedIsWalkable = true;


    // ==================================================================================
    // 3. 
    // ==================================================================================
    public Tile(GridCoords coords, FloorType floorType, PillarType pillarType = PillarType.None)
    {
        Debug.Log($"[Tile] Created new tile at {coords}");
        Coordinate = coords;
        FloorID = floorType;
        PillarID = pillarType;

        // ⺻ : Open
        for (int i = 0; i < 4; i++) _edges[i] = EdgeInfo.Open;

        // Ʈ  ʱȭ (GC )
        _readOnlyItems = _items.AsReadOnly();

        UpdateCache();
    }


    // ==================================================================================
    // 4. ȸ (Getters)
    // ==================================================================================
    public bool IsWalkable => _cachedIsWalkable;
    public ITileOccupant PrimaryUnit => _primaryUnit;

    /// <summary>
    /// []  Ÿ   並 ȯմϴ. (O(1))
    /// ȯ Ʈ foreach ȸϴ  Add/RemoveOccupant ȣǸ ܰ ߻  ֽϴ.
    ///  ȸ ʿϴٸ GetItemsCopy() ϼ.
    /// </summary>
    public IReadOnlyList<ITileOccupant> Items => _readOnlyItems;

    /// <summary>
    /// []  Ʈ 纻 Ͽ ȯմϴ. (O(N))
    /// ȸ  Ʈ    ϼ.
    /// </summary>
    public List<ITileOccupant> GetItemsCopy() => new List<ITileOccupant>(_items);

    public EdgeInfo GetEdge(Direction dir) => _edges[(int)dir];


    // ==================================================================================
    // 5.   (Logic Optimized)
    // ==================================================================================
    public void SetEdge(Direction dir, EdgeInfo newInfo)
    {
        _edges[(int)dir] = newInfo;
    }

    // [ 1] ߺ     ȭ
    public void DamageEdge(Direction dir, float damage)
    {
        EdgeInfo oldEdge = _edges[(int)dir];
        EdgeInfo newEdge = oldEdge.WithDamage(damage);

        // 1. ı (̹   HP 0 )
        if (newEdge.IsDestroyed && oldEdge.CurrentHP > 0)
        {
            //  () ٷ 
            _edges[(int)dir] = EdgeInfo.Open;
            OnEdgeDestroyed?.Invoke(dir, oldEdge);
        }
        // 2. ջ ( )
        else if (newEdge.CurrentHP < oldEdge.CurrentHP)
        {
            _edges[(int)dir] = newEdge;
            OnEdgeDamaged?.Invoke(dir, newEdge);
        }
        // 3. ȭ  (̹ ıǾų  0)
    }


    // ==================================================================================
    // 6.   (Exception Msg & Safety)
    // ==================================================================================
    public void AddOccupant(ITileOccupant occupant)
    {
        if (occupant == null) return;

        switch (occupant.Type)
        {
            case OccupantType.Unit:
                // [ 2]  ޽ ȭ
                if (_primaryUnit != null)
                {
                    throw new InvalidOperationException(
                        $"Cannot add unit to {Coordinate}: already occupied by {_primaryUnit}. " +
                        $"Remove existing unit first using RemoveOccupant().");
                }
                _primaryUnit = occupant;
                break;

            case OccupantType.Item:
                _items.Add(occupant);
                break;

            case OccupantType.Obstacle:
                _obstacles.Add(occupant);
                break;

            default:
                Debug.LogWarning($"Unknown occupant type: {occupant.Type}");
                return;
        }

        occupant.OnBlockingChanged += HandleOccupantStateChange;

        // OnCoverChanged Tile (̵ɼ)  Ƿ  .
        //  Ŵ    .

        UpdateCache();
        occupant.OnAddedToTile(this);
    }

    public void RemoveOccupant(ITileOccupant occupant)
    {
        if (occupant == null)
        {
            Debug.LogWarning($"RemoveOccupant: Occupant is null at {Coordinate}");
            return;
        }

        bool removed = false;

        switch (occupant.Type)
        {
            case OccupantType.Unit:
                if (_primaryUnit == occupant)
                {
                    _primaryUnit = null;
                    removed = true;
                }
                else
                {
                    Debug.LogWarning($"RemoveOccupant: Unit mismatch or empty at {Coordinate}");
                }
                break;

            case OccupantType.Item:
                removed = _items.Remove(occupant);
                if (!removed) Debug.LogWarning($"RemoveOccupant: Item not found at {Coordinate}");
                break;

            case OccupantType.Obstacle:
                removed = _obstacles.Remove(occupant);
                if (!removed) Debug.LogWarning($"RemoveOccupant: Obstacle not found at {Coordinate}");
                break;

            default:
                Debug.LogError($"RemoveOccupant: Unknown type {occupant.Type}");
                return;
        }

        if (removed)
        {
            occupant.OnBlockingChanged -= HandleOccupantStateChange;
            UpdateCache();
            occupant.OnRemovedFromTile(this);
        }
    }


    // ==================================================================================
    // 7.  
    // ==================================================================================
    private void HandleOccupantStateChange(bool isBlocking)
    {
        UpdateCache();
    }

    private void UpdateCache()
    {
        bool oldState = _cachedIsWalkable;

        bool unitBlocking = _primaryUnit != null && _primaryUnit.IsBlockingMovement;
        bool obstacleBlocking = _obstacles.Any(o => o.IsBlockingMovement);

        _cachedIsWalkable = !unitBlocking && !obstacleBlocking;

        if (oldState != _cachedIsWalkable)
        {
            OnWalkableStatusChanged?.Invoke(this);
        }
    }

    /// <summary>
    /// [ ݿ]  (SaveData)κ Ÿ ¸ 
    /// </summary>
    public void LoadFromSaveData(TileSaveData saveData)
    {
        // 1. ⺻  
        this.Coordinate = saveData.Coords;
        this.FloorID = saveData.FloorID;
        this.PillarID = saveData.PillarID;

        // 2. ()  
        if (saveData.Edges != null && saveData.Edges.Length == 4)
        {
            for (int i = 0; i < 4; i++)
            {
                // SavedEdgeInfo -> EdgeInfo ȯ (DataType Ե)
                _edges[i] = saveData.Edges[i].ToEdgeInfo();
            }
        }

        // 3. ĳ 
        UpdateCache();
    }

    public override string ToString() => $"Tile {Coordinate} [{FloorID}]";


}
