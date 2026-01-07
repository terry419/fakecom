using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

public class UnitMovementSystem : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f; // 속도 약간 상향 조정 (기존 3.0 -> 5.0)
    [SerializeField] private float rotateSpeed = 15.0f;

    private Unit _unit;
    private Transform _visualTransform;

    public void Initialize(Unit unit)
    {
        _unit = unit;
        _visualTransform = unit.transform;
    }

    /// <summary>
    /// 주어진 경로를 따라 유닛을 물리적으로 이동시킵니다.
    /// </summary>
    public async UniTask MoveAlongPathAsync(List<GridCoords> path, MapManager mapManager)
    {
        if (path == null || path.Count == 0) return;

        foreach (var nextCoords in path)
        {
            // 1. 논리적 위치 업데이트 (MapManager)
            mapManager.MoveUnit(_unit, nextCoords);

            // 2. 물리적 이동 (Visual)
            Vector3 targetPos = GridUtils.GridToWorld(nextCoords);
            targetPos.y = GetTargetHeight(targetPos.y);

            // 회전
            Vector3 direction = (targetPos - _visualTransform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                RotateTo(targetRot).Forget();
            }

            // 이동 (Lerp/MoveTowards)
            while (Vector3.Distance(_visualTransform.position, targetPos) > 0.05f)
            {
                _visualTransform.position = Vector3.MoveTowards(_visualTransform.position, targetPos, moveSpeed * Time.deltaTime);
                await UniTask.Yield();
            }
            _visualTransform.position = targetPos;
        }
    }

    private async UniTaskVoid RotateTo(Quaternion targetRot)
    {
        float t = 0;
        Quaternion startRot = _visualTransform.rotation;
        while (t < 1f)
        {
            t += Time.deltaTime * rotateSpeed;
            _visualTransform.rotation = Quaternion.Slerp(startRot, targetRot, t);
            await UniTask.Yield();
        }
        _visualTransform.rotation = targetRot;
    }

    private float GetTargetHeight(float surfaceY)
    {
        var collider = GetComponent<Collider>();
        return (collider == null) ? surfaceY : surfaceY + collider.bounds.extents.y;
    }
}