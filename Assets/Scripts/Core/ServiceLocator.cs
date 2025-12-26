using Cysharp.Threading.Tasks; // 비동기 작업(UniTask)을 위해 필요
using System;
using System.Collections.Generic;
using System.Linq; // 리스트를 뒤집거나 합칠 때 사용
using System.Text; // 로그를 예쁘게 모아서 찍을 때 사용
using UnityEngine;

// static class: 이 공구함은 게임 세상에 딱 하나만 존재한다는 뜻입니다.
public static class ServiceLocator
{
    // ==================================================================================
    // 1. 저장소 (공구함 서랍)
    //    Dictionary: 물건을 이름표(Key)로 찾기 위해 씁니다. "망치 줘!" 하면 바로 줍니다.
    //    List: 물건을 넣은 '순서'를 기억하기 위해 씁니다. 나중에 치울 때 순서대로 치우려구요.
    // ==================================================================================

    // [Global] 앱이 켜져있는 동안 계속 살아있는 매니저들
    private static readonly Dictionary<Type, object> _globalServices = new Dictionary<Type, object>();
    private static readonly List<object> _globalOrder = new List<object>();

    // [Session] 전투 한 판 동안만 살아있는 매니저들
    private static readonly Dictionary<Type, object> _sessionServices = new Dictionary<Type, object>();
    private static readonly List<object> _sessionOrder = new List<object>();

    // [Scene] 씬이 바뀔 때마다 사라지는 매니저들
    private static readonly Dictionary<Type, object> _sceneServices = new Dictionary<Type, object>();
    private static readonly List<object> _sceneOrder = new List<object>();


    // ==================================================================================
    // 2. 등록 (Register) - 공구함에 물건 넣기
    // ==================================================================================

    /// <summary>
    /// 매니저를 공구함에 등록합니다.
    /// service: 등록할 매니저 (예: 나 자신 this)
    /// scope: 어느 칸에 넣을지 (Global, Session, Scene)
    /// </summary>
    public static void Register<T>(T service, ManagerScope scope)
    {
        Type type = typeof(T); // 이 녀석의 이름표 (예: "MapManager")

        // 우리가 쓸 서랍(Dictionary)과 명단(List)을 가져옵니다.
        var (targetDict, targetList) = GetContainer(scope);

        // 혹시 이미 등록된 녀석인가요? (중복 체크)
        if (targetDict.ContainsKey(type))
        {
            // 이미 있는데 또 넣으면 에러를 냅니다. (실수 방지)
            throw new InvalidOperationException($"[ServiceLocator] '{type.Name}'는 이미 '{scope}' 칸에 등록되어 있습니다!");
        }

        // 서랍에 넣고, 명단에도 적습니다.
        targetDict.Add(type, service);
        targetList.Add(service);
    }

    /// <summary>
    /// 매니저를 공구함에서 뺍니다. (잘 안 쓰지만 혹시 몰라 만듦)
    /// </summary>
    public static void Unregister<T>(ManagerScope scope)
    {
        Type type = typeof(T);
        var (targetDict, targetList) = GetContainer(scope);

        if (targetDict.ContainsKey(type))
        {
            object service = targetDict[type];
            targetDict.Remove(type);    // 서랍에서 빼고
            targetList.Remove(service); // 명단에서 지움
        }
    }


    // ==================================================================================
    // 3. 조회 (Get) - 공구함에서 물건 꺼내기
    // ==================================================================================

    /// <summary>
    /// 매니저를 찾아서 줍니다.
    /// 찾는 순서: Scene(맨 위) -> Session(중간) -> Global(맨 아래)
    /// </summary>
    public static T Get<T>()
    {
        Type type = typeof(T);

        // 1. Scene 칸 뒤져보기
        if (_sceneServices.TryGetValue(type, out var service)) return (T)service;

        // 2. 없으면 Session 칸 뒤져보기
        if (_sessionServices.TryGetValue(type, out service)) return (T)service;

        // 3. 없으면 Global 칸 뒤져보기
        if (_globalServices.TryGetValue(type, out service)) return (T)service;

        // 4. 다 뒤져도 없으면 에러! (개발자가 실수한 겁니다)
        Debug.LogError($"[ServiceLocator] 큰일났습니다! '{type.Name}'를 찾을 수 없습니다. 등록은 하셨나요?");
        return default;
    }

    /// <summary>
    /// 매니저가 있는지 살짝 확인해봅니다. 없어도 에러를 내지 않습니다. (안전한 확인용)
    /// </summary>
    public static bool TryGet<T>(out T service)
    {
        Type type = typeof(T);
        object result = null;

        // 세 군데 다 뒤져봅니다.
        if (_sceneServices.TryGetValue(type, out result) ||
            _sessionServices.TryGetValue(type, out result) ||
            _globalServices.TryGetValue(type, out result))
        {
            service = (T)result;
            return true; // 찾았다!
        }

        service = default;
        return false; // 없다...
    }

    /// <summary>
    /// 특정 기능(인터페이스)을 가진 녀석들을 몽땅 리스트로 줍니다.
    /// 예: "초기화가 필요한(IInitializable) 애들 다 나와!" 할 때 씁니다.
    /// </summary>
    public static List<TInterface> GetByInterface<TInterface>() where TInterface : class
    {
        // Global -> Session -> Scene 순서대로 싹 긁어모아서 리스트로 만듭니다.
        return _globalServices.Values
            .Concat(_sessionServices.Values)
            .Concat(_sceneServices.Values)
            .OfType<TInterface>()
            .ToList();
    }

    /// <summary>
    /// 이 매니저가 등록되어 있긴 한가요?
    /// </summary>
    public static bool IsRegistered<T>()
    {
        Type type = typeof(T);
        return _sceneServices.ContainsKey(type) ||
               _sessionServices.ContainsKey(type) ||
               _globalServices.ContainsKey(type);
    }


    // ==================================================================================
    // 4. 정리 (Cleanup) - 청소하기
    // ==================================================================================

    /// <summary>
    /// 특정 칸(Scope)을 싹 비웁니다.
    /// 비동기(Async): 청소하는 데 시간이 좀 걸려도 기다려줍니다. (예: 파일 저장 중일 때)
    /// </summary>
    public static async UniTask ClearScopeAsync(ManagerScope scope)
    {
        var (targetDict, targetList) = GetContainer(scope);

        // 비울 게 없으면 그냥 퇴근
        if (targetList.Count == 0) return;

        // [로그 뭉치기] 청소 결과를 하나하나 출력하면 콘솔이 지저분해지니까, 
        // 종이 한 장(StringBuilder)에 내용을 다 적어서 한 번에 보고할 겁니다.
        StringBuilder sbSuccess = new StringBuilder();
        StringBuilder sbFail = new StringBuilder();

        sbSuccess.AppendLine($"<b>[ServiceLocator] '{scope}' 칸 청소 결과 (성공)</b>");
        sbFail.AppendLine($"<b>[ServiceLocator] '{scope}' 칸 청소 결과 (실패)</b>");

        int successCount = 0;
        int failCount = 0;

        // [중요] 거꾸로 청소하기 (LIFO)
        // 나중에 들어온 녀석부터 치워야 안전합니다. 
        // 예: 책을 쌓아뒀으면, 맨 위의 책부터 치워야 무너지지 않겠죠?
        for (int i = targetList.Count - 1; i >= 0; i--)
        {
            var service = targetList[i];

            // "혹시 너 가기 전에 정리할 거(ICleanup) 있니?" 물어봅니다.
            if (service is ICleanup cleanupTarget)
            {
                try
                {
                    // "정리할 시간 줄게, 다 하면 말해." (await)
                    await cleanupTarget.Cleanup();

                    // 성공했다고 기록장에 적음
                    sbSuccess.AppendLine($" - [완료] {service.GetType().Name}");
                    successCount++;
                }
                catch (Exception ex)
                {
                    // 실패하면 빨간 글씨로 기록장에 적음
                    sbFail.AppendLine($" - <color=red>[에러]</color> {service.GetType().Name}: {ex.Message}");
                    failCount++;
                }
            }
        }

        // 이제 진짜로 서랍을 비웁니다.
        targetDict.Clear();
        targetList.Clear();

        // [보고] 적어둔 기록장을 콘솔에 출력합니다.

        // 1. 실패한 게 하나라도 있으면 빨간 에러 로그로 출력
        if (failCount > 0)
        {
            Debug.LogError(sbFail.ToString());
        }

        // 2. 성공한 게 있으면 일반 로그로 출력
        if (successCount > 0)
        {
            Debug.Log(sbSuccess.ToString());
        }
        else
        {
            // 정리할 건 없었고 그냥 비우기만 했다면 간단히 출력
            Debug.Log($"[ServiceLocator] '{scope}' 칸을 비웠습니다. (특별히 정리할 작업은 없었음)");
        }
    }


    // ==================================================================================
    // 5. 도우미 함수 (Helper) - 내부에서만 쓰는 것
    // ==================================================================================

    // 칸 이름(Enum)을 주면, 실제 저장소(Dictionary)와 명단(List)을 꺼내주는 함수입니다.
    private static (Dictionary<Type, object>, List<object>) GetContainer(ManagerScope scope)
    {
        switch (scope)
        {
            case ManagerScope.Global: return (_globalServices, _globalOrder);
            case ManagerScope.Session: return (_sessionServices, _sessionOrder);
            case ManagerScope.Scene: return (_sceneServices, _sceneOrder);
            default: throw new ArgumentException($"그런 칸은 없는데요?: {scope}");
        }
    }
}