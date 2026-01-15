using UnityEngine;
using Cysharp.Threading.Tasks;
using System;
using System.Collections; // Coroutine 사용

public class QTEManager : MonoBehaviour
{
    // [설정] 인스펙터 할당
    [Header("Configuration")]
    [SerializeField] private QTEController _qteController;
    [SerializeField] private ActionModuleSO _defaultModule;
    [SerializeField] private QTETypeSettingsSO _typeSettings; // 타입별 확률 데이터

    // [상태] 실행 중 체크
    private bool _isQTERunning = false;

    private void Awake()
    {
        ServiceLocator.Register<QTEManager>(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        if (ServiceLocator.IsRegistered<QTEManager>())
            ServiceLocator.Unregister<QTEManager>(ManagerScope.Scene);
    }

    // ========================================================================
    // 1. 핵심 로직 (Async/Await)
    // ========================================================================
    public async UniTask<bool> StartQTEAsync(QTEType type)
    {
        // 1. 상태 체크 (중복 실행 방지)
        if (_isQTERunning)
        {
            Debug.LogWarning("[QTEManager] 이미 QTE가 실행 중입니다. 요청 무시됨.");
            return false;
        }

        // 2. 필수 참조 체크
        if (_qteController == null)
        {
            Debug.LogError("[QTEManager] QTEController가 연결되지 않았습니다!");
            return false;
        }

        try
        {
            _isQTERunning = true;
            Debug.Log($"[QTEManager] {type} QTE 시작.");

            // 3. 확률 가져오기
            var (hitChance, critChance) = GetChances(type);

            // 4. 실행 및 대기
            QTEResult result = await _qteController.StartQTEAsync(hitChance, critChance, _defaultModule);

            // 5. 결과 반환
            return result == QTEResult.Success;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[QTEManager] QTE 실행 중 오류 발생: {ex.Message}");
            return false;
        }
        finally
        {
            // 6. 상태 해제
            _isQTERunning = false;
        }
    }

    // 헬퍼: 확률 가져오기
    private (float, float) GetChances(QTEType type)
    {
        if (_typeSettings != null)
            return _typeSettings.GetChances(type);

        // 설정 파일 없으면 기본값
        return (50f, 20f);
    }

    // ========================================================================
    // 2. 호환성 래퍼 (Legacy Callback Support)
    // ========================================================================
    public void StartQTE(QTEType type, Action<bool> onResult)
    {
        StartCoroutine(StartQTECoroutine(type, onResult));
    }

    private IEnumerator StartQTECoroutine(QTEType type, Action<bool> onResult)
    {
        // UniTask -> Coroutine 변환 실행
        var task = StartQTEAsync(type);
        yield return task.ToCoroutine();

        // [Fix] UniTask는 .Result가 없으므로 GetAwaiter().GetResult() 사용
        // (위에서 yield return으로 완료를 기다렸으므로 안전함)
        bool isSuccess = task.GetAwaiter().GetResult();
        onResult?.Invoke(isSuccess);
    }

    // ========================================================================
    // 3. 기존 메서드 (즉시 결과)
    // ========================================================================
    public bool GetQTESuccessInstant(QTEType type)
    {
        var (hit, _) = GetChances(type);
        return UnityEngine.Random.value <= (hit / 100f);
    }
}