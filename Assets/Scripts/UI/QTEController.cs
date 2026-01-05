using UnityEngine;
using System;

public class QTEController : MonoBehaviour
{
    /// <summary>
    /// 실제 QTE UI를 띄우는 메서드.
    /// 현재는 UI가 없으므로 로그만 찍고 바로 '성공(true)' 결과를 돌려줍니다.
    /// </summary>
    public void StartQTE(QTEType type, Action<bool> onResult)
    {
        Debug.Log($"[QTE UI] '{type}' QTE가 화면에 떴다고 가정합니다.");

        // --- 테스트용: 0.5초 뒤에 성공한 것으로 처리 ---
        // UI가 없어도 게임이 멈추지 않게 함
        InvokeResult(onResult, true);
    }

    private async void InvokeResult(Action<bool> onResult, bool result)
    {
        await Cysharp.Threading.Tasks.UniTask.Delay(500); // 0.5초 대기 (연출 느낌)
        Debug.Log($"[QTE UI] 플레이어 성공 입력! (Result: {result})");
        onResult?.Invoke(result);
    }
}