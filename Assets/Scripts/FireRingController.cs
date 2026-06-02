using System.Collections.Generic;
using UnityEngine;

public class FireRingController : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";

    [Header("Orbit")]
    [Tooltip("불덩이가 회전하는 원형 궤도의 지름입니다.")]
    [SerializeField] private float orbitRadius = 3f;
    [Tooltip("초당 회전 각도입니다. 양수는 반시계 방향, 음수는 시계 방향으로 회전합니다.")]
    [SerializeField] private float orbitSpeed = 45f;

    [Header("Damage")]
    [Tooltip("PlayerStatus.CurrentAttack에 곱해질 공격력 배율입니다.")]
    [SerializeField] private float damageMultiplier = 1f;
    [Tooltip("같은 몬스터가 여러 불덩이에 동시에 닿을 때 과도하게 연타되지 않도록 하는 내부 쿨다운입니다.")]
    [SerializeField] private float damageCooldown = 0.35f;

    [Header("Prefab")]
    [SerializeField] private GameObject fireOrbPrefab;

    private readonly List<FireOrbState> fireOrbs = new List<FireOrbState>();
    private readonly Dictionary<int, float> nextDamageTimesByTarget = new Dictionary<int, float>();
    private readonly Collider2D[] overlapResults = new Collider2D[32];

    private PlayerStatus playerStatus;
    private ContactFilter2D enemyContactFilter;
    private float currentAngle;
    private int enemyLayer = -1;
    private int fireOrbCount;

    public float OrbitRadius
    {
        get => orbitRadius;
        set => orbitRadius = Mathf.Max(0f, value);
    }

    public float OrbitSpeed
    {
        get => orbitSpeed;
        set => orbitSpeed = value;
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }

    public GameObject FireOrbPrefab
    {
        get => fireOrbPrefab;
        set
        {
            if (fireOrbPrefab == value)
            {
                return;
            }

            fireOrbPrefab = value;
            RebuildFireOrbs();
        }
    }

    public float DamageCooldown
    {
        get => damageCooldown;
        set => damageCooldown = Mathf.Max(0f, value);
    }

    public int FireOrbCount => fireOrbCount;

    private void Awake()
    {
        CacheReferences();
        ConfigureEnemyContactFilter();
        RebuildFireOrbs();
    }

    private void OnValidate()
    {
        orbitRadius = Mathf.Max(0f, orbitRadius);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        damageCooldown = Mathf.Max(0f, damageCooldown);
        fireOrbCount = Mathf.Max(0, fireOrbCount);
    }

    private void Update()
    {
        if (GameplayPauseManager.IsPaused || fireOrbs.Count <= 0)
        {
            return;
        }

        currentAngle = Mathf.Repeat(currentAngle + orbitSpeed * Time.deltaTime, 360f);
        UpdateFireOrbPositions();
    }

    private void FixedUpdate()
    {
        if (GameplayPauseManager.IsPaused || fireOrbs.Count <= 0)
        {
            return;
        }

        DamageOverlappingEnemies();
    }

    private void OnDestroy()
    {
        ClearFireOrbs();
    }

    public void SetFireOrbCount(int count)
    {
        int clampedCount = Mathf.Max(0, count);
        if (fireOrbCount == clampedCount)
        {
            return;
        }

        fireOrbCount = clampedCount;
        RebuildFireOrbs();
    }

    public void ApplyLevelUpCardValue(float value)
    {
        SetFireOrbCount(Mathf.Max(0, Mathf.RoundToInt(value)));
    }

    private void CacheReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void ConfigureEnemyContactFilter()
    {
        enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        enemyContactFilter = new ContactFilter2D
        {
            useLayerMask = enemyLayer >= 0,
            layerMask = enemyLayer >= 0 ? (LayerMask)(1 << enemyLayer) : default,
            useTriggers = true
        };
    }

    private void RebuildFireOrbs()
    {
        ClearFireOrbs();
        nextDamageTimesByTarget.Clear();

        if (!Application.isPlaying || fireOrbPrefab == null || fireOrbCount <= 0)
        {
            return;
        }

        for (int i = 0; i < fireOrbCount; i++)
        {
            GameObject fireOrb = Instantiate(fireOrbPrefab, transform);
            fireOrb.name = $"{fireOrbPrefab.name}_{i + 1}";
            ConfigureFireOrbPhysics(fireOrb);

            Collider2D orbCollider = fireOrb.GetComponentInChildren<Collider2D>();
            if (orbCollider == null)
            {
                Debug.LogWarning($"Fire orb prefab '{fireOrbPrefab.name}' needs a Collider2D to damage enemies.", this);
            }

            fireOrbs.Add(new FireOrbState(fireOrb.transform, orbCollider));
        }

        UpdateFireOrbPositions();
    }

    private void ClearFireOrbs()
    {
        for (int i = fireOrbs.Count - 1; i >= 0; i--)
        {
            Transform orbTransform = fireOrbs[i].Transform;
            if (orbTransform == null)
            {
                continue;
            }

            if (Application.isPlaying)
            {
                Destroy(orbTransform.gameObject);
            }
            else
            {
                DestroyImmediate(orbTransform.gameObject);
            }
        }

        fireOrbs.Clear();
    }

    private void ConfigureFireOrbPhysics(GameObject fireOrb)
    {
        Rigidbody2D rb = fireOrb.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            return;
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true;
    }

    private void UpdateFireOrbPositions()
    {
        float orbitDistance = orbitRadius * 0.5f;
        int count = fireOrbs.Count;
        if (count <= 0)
        {
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Transform orbTransform = fireOrbs[i].Transform;
            if (orbTransform == null)
            {
                continue;
            }

            float angle = currentAngle + 360f * i / count;
            float radians = angle * Mathf.Deg2Rad;
            orbTransform.localPosition = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0f) * orbitDistance;
            orbTransform.localRotation = Quaternion.identity;
        }
    }

    private void DamageOverlappingEnemies()
    {
        float now = Time.time;
        for (int i = 0; i < fireOrbs.Count; i++)
        {
            Collider2D orbCollider = fireOrbs[i].Collider;
            if (orbCollider == null || !orbCollider.enabled)
            {
                continue;
            }

            int overlapCount = orbCollider.Overlap(enemyContactFilter, overlapResults);
            for (int j = 0; j < overlapCount; j++)
            {
                TryDamageEnemy(overlapResults[j], now);
                overlapResults[j] = null;
            }
        }
    }

    private void TryDamageEnemy(Collider2D enemyCollider, float now)
    {
        if (enemyCollider == null || !IsEnemy(enemyCollider))
        {
            return;
        }

        IDamageable damageable = enemyCollider.GetComponentInParent<IDamageable>();
        if (damageable == null)
        {
            return;
        }

        int cooldownKey = GetCooldownKey(enemyCollider, damageable);
        if (nextDamageTimesByTarget.TryGetValue(cooldownKey, out float nextDamageTime) && now < nextDamageTime)
        {
            return;
        }

        CacheReferences();
        float damage = playerStatus != null ? playerStatus.CalculateDamage(damageMultiplier) : damageMultiplier;
        float appliedDamage = damageable.TakeDamage(damage, playerStatus);
        if (appliedDamage > 0f)
        {
            nextDamageTimesByTarget[cooldownKey] = now + damageCooldown;
        }
    }

    private bool IsEnemy(Collider2D enemyCollider)
    {
        if (enemyLayer < 0)
        {
            return false;
        }

        if (enemyCollider.gameObject.layer == enemyLayer)
        {
            return true;
        }

        Rigidbody2D attachedRigidbody = enemyCollider.attachedRigidbody;
        return attachedRigidbody != null && attachedRigidbody.gameObject.layer == enemyLayer;
    }

    private static int GetCooldownKey(Collider2D enemyCollider, IDamageable damageable)
    {
        Component damageableComponent = damageable as Component;
        if (damageableComponent != null)
        {
            return damageableComponent.GetInstanceID();
        }

        Rigidbody2D attachedRigidbody = enemyCollider.attachedRigidbody;
        return attachedRigidbody != null ? attachedRigidbody.GetInstanceID() : enemyCollider.GetInstanceID();
    }

    private readonly struct FireOrbState
    {
        public FireOrbState(Transform transform, Collider2D collider)
        {
            Transform = transform;
            Collider = collider;
        }

        public Transform Transform { get; }
        public Collider2D Collider { get; }
    }
}
