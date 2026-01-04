using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Text;

public class SceneInitializer : MonoBehaviour
{
    public async UniTask InitializeSceneAsync(InitializationContext context, StringBuilder log)
    {
        log.AppendLine("   [Scene] Starting Scene Initialization...");

        // 0. Asset Verification
        if (context.MapData != null)
            log.AppendLine("     - MapData Verified (Ready to use)");
        else
            log.AppendLine("     - No MapData provided (Empty Scene?)");

        // 1. Manager Initialization Sequence
        var initSequence = new Type[]
        {
            typeof(TileDataManager),
            typeof(MapManager),
            typeof(TilemapGenerator),
            typeof(EnvironmentManager),
            typeof(UnitManager),
            typeof(TurnManager),
            typeof(CameraController) // 카메라가 있어야 Raycast 가능
        };

        foreach (var managerType in initSequence)
        {
            var managerObj = FindObjectOfType(managerType) as MonoBehaviour;

            if (managerObj != null && managerObj is IInitializable initializable)
            {
                try
                {
                    log.Append($"     - {managerType.Name}...");
                    await initializable.Initialize(context);
                    log.AppendLine(" ✓");
                }
                catch (Exception ex)
                {
                    log.AppendLine($" ✗");
                    throw new BootstrapException($"Failed to initialize {managerType.Name}: {ex.Message}", ex);
                }
            }
        }

        // ========================================================================
        // [Fix] 2. Map Generation Call (여기가 빠져 있었습니다!)
        // ========================================================================
        var tilemapGenerator = FindObjectOfType<TilemapGenerator>();
        if (tilemapGenerator != null)
        {
            try
            {
                log.Append("     - Generating Tilemap Visuals...");
                // MapManager는 이미 초기화되었으므로 데이터가 존재함. 비주얼 생성 시작.
                await tilemapGenerator.GenerateAsync();
                log.AppendLine(" ✓");
            }
            catch (Exception ex)
            {
                log.AppendLine($" ✗ {ex.Message}");
                throw new BootstrapException("Failed to generate tilemap visuals.", ex);
            }
        }
        else
        {
            log.AppendLine("     - Warning: TilemapGenerator not found.");
        }
        // ========================================================================

        log.AppendLine("   [Scene] Scene Initialization Complete.");
    }
}