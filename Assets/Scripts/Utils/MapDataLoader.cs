// Assets/Scripts/Utils/MapDataLoader.cs
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace YCOM.Utils
{
    public static class MapDataLoader
    {
        public static async UniTask<MapDataSO> LoadMapDataAsync(MissionDataSO mission)
        {
            if (mission == null) return null;

            // 1. 유효성 검사
            if (mission.MapDataRef == null || !mission.MapDataRef.RuntimeKeyIsValid())
            {
                Debug.LogError($"[MapDataLoader] Invalid Reference: {mission.name}");
                return null;
            }

            // 2. 이미 깔끔하게 로드된 상태면 바로 리턴
            if (mission.MapDataRef.Asset != null)
            {
                return mission.MapDataRef.Asset as MapDataSO;
            }

            // [핵심 Fix] 좀비 핸들 처리
            // Asset은 없는데 Handle이 유효하다? -> "이전 실행의 찌꺼기"입니다.
            // 이걸 Release 해주지 않으면 LoadAssetAsync가 "중복 로드"라며 에러를 뱉습니다.
            if (mission.MapDataRef.OperationHandle.IsValid())
            {
                // Debug.LogWarning($"[MapDataLoader] Stale handle detected for {mission.name}. Releasing...");
                mission.MapDataRef.ReleaseAsset(); // 강제 초기화
            }

            // 3. 이제 깨끗한 상태에서 로드 시작
            var handle = mission.MapDataRef.LoadAssetAsync<MapDataSO>();

            try
            {
                await handle.ToUniTask();

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    Debug.LogError($"[MapDataLoader] Load Failed: {mission.name}");
                    // 실패했다면 핸들 정리
                    if (handle.IsValid()) Addressables.Release(handle);
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[MapDataLoader] Exception: {ex.Message}");
                // 예외 발생 시에도 핸들 정리
                if (mission.MapDataRef.OperationHandle.IsValid()) mission.MapDataRef.ReleaseAsset();
                return null;
            }
        }
    }
}