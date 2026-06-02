using UnityEngine;

public class FireGroundController : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";

    [Header("Fire Ground")]
    [Tooltip("플레이어 위치에 생성할 불장판 프리팹입니다.")]
    [SerializeField] private GameObject fireGroundPrefab;
    [Tooltip("1레벨 기준 불장판 소환 주기입니다.")]
    [SerializeField] private float baseSpawnInterval = 2f;
    [Tooltip("레벨이 오를 때마다 감소하는 소환 주기입니다.")]
    [SerializeField] private float intervalDecreasePerLevel = 0.25f;
    [Tooltip("소환 주기의 최소값입니다.")]
    [SerializeField] private float minimumSpawnInterval = 0.25f;
    [Tooltip("불장판이 유지되는 시간입니다. 레벨이 올라도 변하지 않습니다.")]
    [SerializeField] private float duration = 1f;
    [Tooltip("1레벨 기준 불장판 지름입니다.")]
    [SerializeField] private float baseDiameter = 3f;
    [Tooltip("레벨이 오를 때마다 처음 지름에 적용되는 배율입니다.")]
    [SerializeField] private float diameterMultiplierPerLevel = 1.25f;
    [Tooltip("불장판이 FixedUpdate마다 주는 고정 피해량입니다.")]
    [SerializeField] private float damage = 1f;
    [Tooltip("피해를 받을 대상 레이어입니다. 기본값은 Enemy 레이어입니다.")]
    [SerializeField] private LayerMask affectsLayers;

    private PlayerStatus playerStatus;
    private int skillLevel;
    private float nextSpawnTime;

    public int SkillLevel => skillLevel;
    public float CurrentSpawnInterval => Mathf.Max(minimumSpawnInterval, baseSpawnInterval - intervalDecreasePerLevel * Mathf.Max(0, skillLevel - 1));
    public float CurrentDiameter => baseDiameter * Mathf.Pow(diameterMultiplierPerLevel, Mathf.Max(0, skillLevel - 1));

    public GameObject FireGroundPrefab
    {
        get => fireGroundPrefab;
        set => fireGroundPrefab = value;
    }

    public float BaseSpawnInterval
    {
        get => baseSpawnInterval;
        set => baseSpawnInterval = Mathf.Max(0f, value);
    }

    public float Duration
    {
        get => duration;
        set => duration = Mathf.Max(0.01f, value);
    }

    public float BaseDiameter
    {
        get => baseDiameter;
        set => baseDiameter = Mathf.Max(0f, value);
    }

    public float Damage
    {
        get => damage;
        set => damage = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureDefaultLayerMask();
        nextSpawnTime = Time.time;
    }

    private void OnValidate()
    {
        baseSpawnInterval = Mathf.Max(0f, baseSpawnInterval);
        intervalDecreasePerLevel = Mathf.Max(0f, intervalDecreasePerLevel);
        minimumSpawnInterval = Mathf.Max(0f, minimumSpawnInterval);
        duration = Mathf.Max(0.01f, duration);
        baseDiameter = Mathf.Max(0f, baseDiameter);
        diameterMultiplierPerLevel = Mathf.Max(0f, diameterMultiplierPerLevel);
        damage = Mathf.Max(0f, damage);
    }

    private void Update()
    {
        if (GameplayPauseManager.IsPaused || skillLevel <= 0)
        {
            return;
        }

        if (Time.time < nextSpawnTime)
        {
            return;
        }

        SpawnFireGround();
        nextSpawnTime = Time.time + CurrentSpawnInterval;
    }

    public void ApplyLevelUpCardValue(float value)
    {
        int newLevel = Mathf.Max(1, Mathf.RoundToInt(value));
        skillLevel = Mathf.Max(skillLevel, newLevel);
        nextSpawnTime = Mathf.Min(nextSpawnTime, Time.time);
    }

    private void SpawnFireGround()
    {
        GameObject fireGroundObject = fireGroundPrefab != null
            ? Instantiate(fireGroundPrefab, transform.position, Quaternion.identity)
            : new GameObject("FireGround");

        fireGroundObject.transform.position = transform.position;
        FireGroundArea fireGroundArea = fireGroundObject.GetComponent<FireGroundArea>();
        if (fireGroundArea == null)
        {
            fireGroundArea = fireGroundObject.AddComponent<FireGroundArea>();
        }

        CacheReferences();
        fireGroundArea.Initialize(playerStatus, damage, CurrentDiameter, duration, affectsLayers);
    }

    private void CacheReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void ConfigureDefaultLayerMask()
    {
        if (affectsLayers.value != 0)
        {
            return;
        }

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer >= 0)
        {
            affectsLayers = 1 << enemyLayer;
        }
    }
}
