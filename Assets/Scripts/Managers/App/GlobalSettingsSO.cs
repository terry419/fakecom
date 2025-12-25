using UnityEngine;

[CreateAssetMenu(fileName = "GlobalSettings", menuName = "Data/Global Settings")]
public class GlobalSettingsSO : ScriptableObject
{
    [Header("Audio")]
    public float MasterVolume = 1.0f;

    [Header("System")]
    public string GameVersion = "1.0.0";
}