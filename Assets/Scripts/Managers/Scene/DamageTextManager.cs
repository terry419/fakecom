using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;

public class DamageTextManager : MonoBehaviour, IInitializable
{
    [Header("Resource Config")]
    [SerializeField] private AssetReferenceGameObject _damageTextRef;

    [Header("Pool Settings")]
    [SerializeField] private int _defaultPoolSize = 20;
    [SerializeField] private int _maxPoolSize = 100;

    [Header("Visual Config")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _critColor = Color.yellow;
    [SerializeField] private Color _missColor = Color.gray;
    [SerializeField] private float _normalScale = 1.0f;
    [SerializeField] private float _critScale = 1.5f;

    private GameObject _prefab;
    private IObjectPool<DamageText> _pool;
    private Transform _poolContainer;
    private bool _isReady = false;

    private void Awake()
    {
        ServiceLocator.Register(this, ManagerScope.Scene);
    }

    private void OnDestroy()
    {
        if (_prefab != null) Addressables.Release(_prefab);
        ServiceLocator.Unregister<DamageTextManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        // 계층 구조 정리용 컨테이너 생성
        GameObject containerGO = new GameObject("DamageTextPool_Container");
        _poolContainer = containerGO.transform;
        _poolContainer.SetParent(this.transform);

        // 1. 프리팹 로드 (문법 수정: 프로퍼티 호출)
        if (_damageTextRef != null && _damageTextRef.RuntimeKeyIsValid())
        {
            try
            {
                _prefab = await _damageTextRef.LoadAssetAsync<GameObject>().ToUniTask();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DamageTextManager] Load Failed: {e.Message}");
                return;
            }
        }

        if (_prefab == null) return;

        // 2. 오브젝트 풀 초기화
        _pool = new ObjectPool<DamageText>(
            createFunc: CreateText,
            actionOnGet: OnGetText,
            actionOnRelease: OnReleaseText, // 여기서 리셋 처리
            actionOnDestroy: OnDestroyText,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize
        );

        _isReady = true;
    }

    private DamageText CreateText()
    {
        GameObject obj = Instantiate(_prefab, _poolContainer);
        DamageText dt = obj.GetComponent<DamageText>();
        dt.SetPool(_pool);
        return dt;
    }

    private void OnGetText(DamageText dt) => dt.gameObject.SetActive(true);

    private void OnReleaseText(DamageText dt)
    {
        // IMPORTANT: 풀 반환 시점에만 상태 초기화 호출
        dt.ResetState();
        dt.gameObject.SetActive(false);
    }

    private void OnDestroyText(DamageText dt) => Destroy(dt.gameObject);

    public void ShowDamage(Vector3 worldPos, int damage, bool isCrit, bool isMiss)
    {
        if (!_isReady) return;

        DamageText dt = _pool.Get();
        dt.transform.position = worldPos + Vector3.up * 1.5f;

        Color color = isMiss ? _missColor : (isCrit ? _critColor : _normalColor);
        float scale = isMiss ? _normalScale : (isCrit ? _critScale : _normalScale);

        dt.Play(damage, color, scale, isMiss);
    }
}