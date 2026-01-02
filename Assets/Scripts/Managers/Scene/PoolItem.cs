using UnityEngine;

public class PoolItem : MonoBehaviour
{
    public Renderer MainRenderer;
    public PoolType Type; // 생성 시 자동 할당

    public enum PoolType { Reachable, Path, Unreachable }

    private void Awake()
    {
        if (MainRenderer == null) MainRenderer = GetComponentInChildren<Renderer>();
    }
}