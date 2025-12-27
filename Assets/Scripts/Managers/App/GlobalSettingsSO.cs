using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "Data/Global Settings")]
public class GlobalSettingsSO : ScriptableObject
{

    [Header("Audio")]
    [Range(0f, 1f)] public float MasterVolume = 1.0f;
    [Range(0f, 1f)] public float BGMVolume = 0.8f;
    [Range(0f, 1f)] public float SFXVolume = 1.0f;

    [Header("Graphics")]
    public bool IsFullScreen = true;
    public int TargetFrameRate = 60;
}