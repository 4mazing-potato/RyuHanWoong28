using UnityEngine;

public class MonsterController : MonoBehaviour, IProjectileDamageable
{
    private const string PlayerTag = "Player";
    private const string EnemyLayerName = "Enemy";

    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float maxHealth = 3f;

    private Transform target;
    private MonsterSpawnManager spawnManager;
    private int spawnRuleIndex = -1;
    private float currentHealth;
    private bool isRegisteredWithSpawner;

    public float MoveSpeed
    {
        get => moveSpeed;
        set => moveSpeed = Mathf.Max(0f, value);
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        AssignEnemyLayerIfAvailable();
        EnsureProjectileCollisionComponents();
    }

    private void Update()
    {
        if (target == null)
        {
            FindPlayerTarget();
            if (target == null)
            {
                return;
            }
        }

        Vector3 direction = target.position - transform.position;
        direction.z = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.position += direction.normalized * (moveSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (isRegisteredWithSpawner && spawnManager != null)
        {
            spawnManager.UnregisterMonster(spawnRuleIndex);
        }
    }

    public void Initialize(Transform playerTarget, MonsterSpawnManager owner, int ruleIndex)
    {
        target = playerTarget;
        spawnManager = owner;
        spawnRuleIndex = ruleIndex;
        isRegisteredWithSpawner = owner != null && ruleIndex >= 0;
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= Mathf.Max(0f, damage);
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        target = player != null ? player.transform : null;
    }

    private void AssignEnemyLayerIfAvailable()
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer >= 0)
        {
            gameObject.layer = enemyLayer;
        }
    }

    private void EnsureProjectileCollisionComponents()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
        }
    }
}
