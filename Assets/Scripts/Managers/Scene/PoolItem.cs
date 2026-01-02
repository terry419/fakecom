using UnityEngine;

public class PoolItem : MonoBehaviour
{
    public Renderer MainRenderer;
    public PoolType Type; //

    public enum PoolType { Reachable, Path, Unreachable }

    private void Awake()
    {
        if (MainRenderer == null) MainRenderer = GetComponentInChildren<Renderer>();
    }
}
