using UnityEngine;
using UnityEngine.AddressableAssets;

public abstract class UnitDataSO : ScriptableObject
{
    [Header("1. Identity")]
    public string UnitID;
    public string UnitName;
    public ClassType Role;

    [Header("2. Visual")]
    public AssetReferenceGameObject ModelPrefab;
    public AssetReferenceGameObject HitVFX;


    [Header("3. Base Stats")]
    public int MaxHP;
    public int MaxAP = 2;
    public int Mobility;
    public int Agility;
    public int Range;
    public int Aim;
    public int Evasion;

    [Range(0f, 100f)]
    public float CritChance = 5f;

    [Header("4. Neural Sync")]
    public float BaseSurvivalChance = 5f;
    public float BaseNeuralSync = 100f;
    public float BaseOverclockChance = 5f;

    [Header("Animation Settings")]
    [Tooltip("사망 시 Animator에 전달할 Trigger 파라미터 이름")]
    public string deathAnimationTrigger = "Die";

    [Tooltip("사망 애니메이션의 State 이름 (재생 대기용)")]
    public string deathStateName = "Death";
}