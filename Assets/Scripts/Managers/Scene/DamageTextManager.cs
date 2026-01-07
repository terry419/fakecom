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

    private void Awake() => ServiceLocator.Register(this, ManagerScope.Scene);
    private void OnDestroy()
    {
        if (_prefab != null) Addressables.Release(_prefab);
        ServiceLocator.Unregister<DamageTextManager>(ManagerScope.Scene);
    }

    public async UniTask Initialize(InitializationContext context)
    {
        GameObject containerGO = new GameObject("DamageTextPool_Container");
        _poolContainer = containerGO.transform;
        _poolContainer.SetParent(this.transform);

        if (_damageTextRef != null && _damageTextRef.RuntimeKeyIsValid())
        {
            try
            {
                _prefab = await _damageTextRef.LoadAssetAsync<GameObject>().ToUniTask();
                Debug.Log($"[DamageTextManager] 프리팹 로드 성공: {_prefab.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[DamageTextManager] Load Failed: {e.Message}");
                return;
            }
        }
        else
        {
            Debug.LogError("[DamageTextManager] Addressable Reference가 설정되지 않았습니다!");
            return;
        }

        if (_prefab == null) return;

        _pool = new ObjectPool<DamageText>(
            createFunc: CreateText,
            actionOnGet: OnGetText,
            actionOnRelease: OnReleaseText,
            actionOnDestroy: OnDestroyText,
            defaultCapacity: _defaultPoolSize,
            maxSize: _maxPoolSize
        );

        _isReady = true;
        Debug.Log("[DamageTextManager] 시스템 준비 완료.");
    }

    private DamageText CreateText()
    {
        GameObject obj = Instantiate(_prefab, _poolContainer);
        DamageText dt = obj.GetComponent<DamageText>();

        // [LOG 5] 실제 생성 확인
        if (dt == null) Debug.LogError("[DamageTextManager] 프리팹에 DamageText 컴포넌트가 없습니다!");

        dt.SetPool(_pool);
        return dt;
    }

    private void OnGetText(DamageText dt) => dt.gameObject.SetActive(true);
    private void OnReleaseText(DamageText dt)
    {
        dt.ResetState();
        dt.gameObject.SetActive(false);
    }
    private void OnDestroyText(DamageText dt) => Destroy(dt.gameObject);

    public void ShowDamage(Vector3 worldPos, int damage, bool isCrit, bool isMiss)
    {
        // [LOG 6] 호출 확인
        Debug.Log($"[DamageTextManager] ShowDamage 호출됨. 준비상태: {_isReady}");

        if (!_isReady)
        {
            Debug.LogWarning("[DamageTextManager] 아직 초기화되지 않아서 무시됨.");
            return;
        }

        DamageText dt = _pool.Get();
        dt.transform.position = worldPos + Vector3.up * 1.5f;

        Color color = isMiss ? _missColor : (isCrit ? _critColor : _normalColor);
        float scale = isMiss ? _normalScale : (isCrit ? _critScale : _normalScale);

        dt.Play(damage, color, scale, isMiss);

        // [LOG 7] 오브젝트 상태 확인
        Debug.Log($"[DamageTextManager] 텍스트 활성화됨 at {dt.transform.position}. Active: {dt.gameObject.activeSelf}");
    }
}