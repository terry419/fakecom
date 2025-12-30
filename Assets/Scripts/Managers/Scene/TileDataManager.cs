using UnityEngine;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System;
using System.Linq;

public class TileDataManager : MonoBehaviour, IInitializable
{
    [SerializeField] private AssetReferenceT<MapEditorSettingsSO> _visualSettingsRef;
    [SerializeField] private AssetReferenceT<TileDataTableSO> _logicSettingsRef;

    private Dictionary<FloorType, GameObject> _floorVisuals;
    private Dictionary<PillarType, GameObject> _pillarVisuals;
    private Dictionary<FloorType, TileLogicData> _floorLogics;
    private Dictionary<PillarType, PillarLogicData> _pillarLogics;

    private bool _isInitialized = false;

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Global);

    private void OnDestroy()
    {
        _floorVisuals?.Clear();
        _pillarVisuals?.Clear();
        _floorLogics?.Clear();
        _pillarLogics?.Clear();
        
        if (_visualSettingsRef.IsValid()) Addressables.Release(_visualSettingsRef);
        if (_logicSettingsRef.IsValid()) Addressables.Release(_logicSettingsRef);
        
        ServiceLocator.Unregister<TileDataManager>(ManagerScope.Global);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        _floorVisuals = new Dictionary<FloorType, GameObject>();
        _pillarVisuals = new Dictionary<PillarType, GameObject>();
        _floorLogics = new Dictionary<FloorType, TileLogicData>();
        _pillarLogics = new Dictionary<PillarType, PillarLogicData>();

        var (visualSettings, logicSettings) = await UniTask.WhenAll(
            _visualSettingsRef.LoadAssetAsync().ToUniTask(),
            _logicSettingsRef.LoadAssetAsync().ToUniTask()
        );

        if (visualSettings == null)
            throw new BootstrapException("Failed to load MapEditorSettingsSO.");
        if (logicSettings == null)
            throw new BootstrapException("Failed to load TileDataTableSO.");
        
        if (visualSettings.FloorMappings.Count == 0)
            Debug.LogWarning("MapEditorSettingsSO has no FloorMappings.");
        if (logicSettings.FloorLogics.Count == 0)
            Debug.LogWarning("TileDataTableSO has no FloorLogics.");
            
        if (visualSettings.PillarMappings.Count == 0)
            Debug.LogWarning("MapEditorSettingsSO has no PillarMappings.");
        if (logicSettings.PillarLogics.Count == 0)
            Debug.LogWarning("TileDataTableSO has no PillarLogics.");

        foreach (var mapping in visualSettings.FloorMappings)
        {
            if (!_floorVisuals.ContainsKey(mapping.type))
                _floorVisuals.Add(mapping.type, mapping.prefab);
        }
        foreach (var mapping in visualSettings.PillarMappings)
        {
            if (!_pillarVisuals.ContainsKey(mapping.type))
                _pillarVisuals.Add(mapping.type, mapping.prefab);
        }
        
        foreach (var logicData in logicSettings.FloorLogics)
        {
            if (!_floorLogics.ContainsKey(logicData.Type))
                _floorLogics.Add(logicData.Type, logicData);
        }
        foreach (var logicData in logicSettings.PillarLogics)
        {
            if (!_pillarLogics.ContainsKey(logicData.Type))
                _pillarLogics.Add(logicData.Type, logicData);
        }

        ValidateConsistency();

        _isInitialized = true;
        Debug.Log($"[TileDataManager] Initialized. Floors(V:{_floorVisuals.Count}, L:{_floorLogics.Count}), " +
                  $"Pillars(V:{_pillarVisuals.Count}, L:{_pillarLogics.Count})");
    }

    private void ValidateConsistency()
    {
        var visualFloorTypes = _floorVisuals.Keys.ToHashSet();
        var logicFloorTypes = _floorLogics.Keys.ToHashSet();
        
        var missingFloorLogic = visualFloorTypes.Except(logicFloorTypes);
        if (missingFloorLogic.Any())
            Debug.LogWarning($"[TileDataManager] Missing Logics for FloorTypes: {string.Join(", ", missingFloorLogic)}");

        var missingFloorVisual = logicFloorTypes.Except(visualFloorTypes);
        if (missingFloorVisual.Any())
            Debug.LogWarning($"[TileDataManager] Missing Visuals for FloorTypes: {string.Join(", ", missingFloorVisual)}");

        var visualPillarTypes = _pillarVisuals.Keys.ToHashSet();
        var logicPillarTypes = _pillarLogics.Keys.ToHashSet();

        var missingPillarLogic = visualPillarTypes.Except(logicPillarTypes);
        if (missingPillarLogic.Any())
            Debug.LogWarning($"[TileDataManager] Missing Logics for PillarTypes: {string.Join(", ", missingPillarLogic)}");

        var missingPillarVisual = logicPillarTypes.Except(visualPillarTypes);
        if (missingPillarVisual.Any())
            Debug.LogWarning($"[TileDataManager] Missing Visuals for PillarTypes: {string.Join(", ", missingPillarVisual)}");
    }

    // --- Public API ---
    public bool TryGetFloorVisual(FloorType type, out GameObject prefab)
    {
        prefab = null;
        if (!_isInitialized) return false;
        return _floorVisuals.TryGetValue(type, out prefab);
    }

    public bool TryGetFloorLogic(FloorType type, out TileLogicData logicData)
    {
        logicData = default;
        if (!_isInitialized) return false;
        return _floorLogics.TryGetValue(type, out logicData);
    }
    
    public bool TryGetPillarVisual(PillarType type, out GameObject prefab)
    {
        prefab = null;
        if (!_isInitialized) return false;
        return _pillarVisuals.TryGetValue(type, out prefab);
    }

    public bool TryGetPillarLogic(PillarType type, out PillarLogicData logicData)
    {
        logicData = default;
        if (!_isInitialized) return false;
        return _pillarLogics.TryGetValue(type, out logicData);
    }
}
