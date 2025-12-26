using UnityEngine;
using System;

[Serializable]
public struct SpawnPointData
{
    public Vector3 Position; // 배치 좌표
    public TeamType Team;    // 아군/적군 구분
    public float YRotation;  // 바라보는 방향 (Y축 회전)
}