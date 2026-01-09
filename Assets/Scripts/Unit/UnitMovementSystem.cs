using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using System;

public class UnitMovementSystem : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5.0f;
    [Tooltip("회전 속도 (초당 각도)")]
    [SerializeField] private float rotateSpeed = 360.0f;

    [Tooltip("셀 단위 텔레포트 임계값 (이 값 * 셀 크기보다 멀면 순간이동)")]
    [SerializeField] private float jumpThresholdCells = 1.5f;

    [Tooltip("포탈(텔레포트) 도착 후 대기 시간 (초). 카메라가 따라올 시간을 줍니다.")]
    [SerializeField] private float portalWaitTime = 0.4f;

    [Tooltip("높이 미세 조정값 (Y축만 사용, 기본적으로 Collider 반 높이가 자동 적용됨)")]
    [SerializeField] private float pivotOffsetY = 0.0f;

    private Unit _unit;
    private Transform _visualTransform;
    private Collider _collider;

    // 카메라 컨트롤러 안전 접근
    private CameraController _cameraController;
    private CameraController CameraCtrl
    {
        get
        {
            if (_cameraController == null)
            {
                if (!ServiceLocator.TryGet(out _cameraController))
                {
                    _cameraController = FindObjectOfType<CameraController>();
                }
            }
            return _cameraController;
        }
    }

    private CancellationTokenSource _moveCts;
    private float _jumpThresholdSq;

    private const float STOP_DISTANCE = 0.05f;
    private const float STOP_DISTANCE_SQ = STOP_DISTANCE * STOP_DISTANCE;

    public void Initialize(Unit unit)
    {
        _unit = unit;
        _visualTransform = unit.transform;
        _collider = unit.GetComponent<Collider>();

        float threshold = jumpThresholdCells * GridUtils.CELL_SIZE;
        _jumpThresholdSq = threshold * threshold;
    }

    public async UniTask MoveAlongPathAsync(List<GridCoords> path, MapManager mapManager, CancellationToken externalCancellationToken = default)
    {
        if (path == null || path.Count == 0) return;

        CancelAndDisposeMovement();
        _moveCts = new CancellationTokenSource();

        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalCancellationToken, _moveCts.Token))
        {
            try
            {
                await MoveRoutine(path, mapManager, linkedCts.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[UnitMovement] Unexpected error: {ex}");
            }
        }
    }

    private void CancelAndDisposeMovement()
    {
        if (_moveCts != null)
        {
            if (!_moveCts.IsCancellationRequested) _moveCts.Cancel();
            _moveCts.Dispose();
            _moveCts = null;
        }
    }

    private async UniTask MoveRoutine(List<GridCoords> path, MapManager mapManager, CancellationToken token)
    {
        foreach (var nextCoords in path)
        {
            token.ThrowIfCancellationRequested();

            GridCoords prevCoords = _unit.Coordinate;
            mapManager.MoveUnit(_unit, nextCoords);

            Vector3 targetPos = GridUtils.GridToWorld(nextCoords);
            targetPos.y += GetHeightOffset();

            bool isLevelChange = (prevCoords.y != nextCoords.y);
            float distSq = (new Vector3(_visualTransform.position.x, 0, _visualTransform.position.z) -
                            new Vector3(targetPos.x, 0, targetPos.z)).sqrMagnitude;
            bool isLongJump = distSq > _jumpThresholdSq;

            if (isLevelChange || isLongJump)
            {
                // [Teleport] 유닛 즉시 이동
                _visualTransform.position = targetPos;

                // [Fix] 카메라 위치 이동 및 각도 초기화 (Recenter)
                if (CameraCtrl != null)
                {
                    CameraCtrl.RecenterCamera(targetPos);
                }

                int delayMs = (int)(portalWaitTime * 1000f);
                await UniTask.Delay(delayMs, cancellationToken: token);
            }
            else
            {
                // [Walk] 일반 이동
                await MoveToTarget(targetPos, token);
            }
        }
    }

    private async UniTask MoveToTarget(Vector3 targetPos, CancellationToken token)
    {
        Vector3 direction = (targetPos - _visualTransform.position).normalized;
        Quaternion targetRot = _visualTransform.rotation;

        if (direction.sqrMagnitude > 0.001f)
        {
            targetRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        }

        while ((_visualTransform.position - targetPos).sqrMagnitude > STOP_DISTANCE_SQ)
        {
            token.ThrowIfCancellationRequested();

            Vector3 newPos = Vector3.MoveTowards(
                _visualTransform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );

            if (newPos == _visualTransform.position || newPos == targetPos)
            {
                _visualTransform.position = targetPos;
                break;
            }

            _visualTransform.position = newPos;
            _visualTransform.rotation = Quaternion.RotateTowards(
                _visualTransform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );

            await UniTask.Yield();
        }

        _visualTransform.position = targetPos;
        _visualTransform.rotation = targetRot;
    }

    private float GetHeightOffset()
    {
        float offset = 0f;
        if (_collider != null)
        {
            offset = _collider.bounds.extents.y;
        }
        return offset + pivotOffsetY;
    }

    private void OnDisable() => CancelAndDisposeMovement();
    private void OnDestroy() => CancelAndDisposeMovement();
}