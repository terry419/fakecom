using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks; // 필수
using System;

namespace YCOM.Utils
{
    public static class MapDataLoader
    {
        private const float DEFAULT_TIMEOUT_SEC = 30f;

        public static async UniTask<MapDataSO> LoadMapDataAsync(MissionDataSO mission, float? timeoutSec = null)
        {
            if (mission == null) throw new ArgumentNullException(nameof(mission));

            float timeout = timeoutSec ?? DEFAULT_TIMEOUT_SEC;
            var timeoutSpan = TimeSpan.FromSeconds(timeout);

            AsyncOperationHandle<MapDataSO> handle = default;

            try
            {
                handle = mission.MapDataRef.LoadAssetAsync();

                // [Fix] 정적 메서드가 아닌 '확장 메서드' 방식으로 호출해야 합니다.
                // handle.ToUniTask()로 만든 태스크 뒤에 .Timeout()을 붙입니다.
                var mapData = await handle.ToUniTask().Timeout(timeoutSpan);

                if (mapData == null)
                    throw new Exception($"Loaded MapData is null for mission: {mission.name}");

                return mapData;
            }
            catch (TimeoutException ex)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new TimeoutException($"MapData Load Timeout ({timeout}s) for mission: {mission.name}", ex);
            }
            catch (Exception ex)
            {
                if (handle.IsValid()) Addressables.Release(handle);
                throw new Exception($"MapData Load Failed: {ex.Message}", ex);
            }
        }
    }
}