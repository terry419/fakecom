using UnityEngine;
using System;

public class QTEManager : MonoBehaviour
{
    // [수정] 필요할 때 가져오는 프로퍼티 (게으른 로딩)
    private GlobalSettingsSO Settings => ServiceLocator.Get<GlobalSettingsSO>();

    [Header("--- Execution References ---")]
    [SerializeField] private QTEController qteController;

    private void Awake()
    {
        ServiceLocator.Register<QTEManager>(this, ManagerScope.Scene);
    }

    // Start() 제거함! (타이밍 이슈 원천 차단)

    public bool GetQTESuccessInstant(QTEType type)
    {
        if (Settings == null) return true; // 안전장치

        float chance = 0.5f;
        switch (type)
        {
            case QTEType.Survival: chance = Settings.probSurvival; break;
            case QTEType.AttackCrit: chance = Settings.probAttackCrit; break;
            case QTEType.EnemyCrit: chance = Settings.probEnemyCrit; break;
            case QTEType.SynchroPulse: chance = Settings.probSynchroPulse; break;
        }
        return UnityEngine.Random.value <= chance;
    }

    public void StartQTE(QTEType type, Action<bool> onResult)
    {
        if (Settings == null)
        {
            onResult?.Invoke(true);
            return;
        }

        Debug.Log($"[QTEManager] {type} 타입의 QTE 시작 요청.");

        if (qteController != null)
        {
            qteController.StartQTE(type, onResult);
        }
        else
        {
            onResult?.Invoke(true);
        }
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<QTEManager>())
            ServiceLocator.Unregister<QTEManager>(ManagerScope.Scene);
    }
}
