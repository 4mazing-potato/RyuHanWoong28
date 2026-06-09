using System;
using UnityEngine;
using UnityEngine.Serialization;

public class TreasureBoxController : MonoBehaviour, IProjectileDamageable
{
    private const string EnemyLayerName = "Enemy";

    [FormerlySerializedAs("maxHealth")]
    [SerializeField] private float maxHP = 10f;
    [Header("Drops")]
    [SerializeField] private DropOption[] dropOptions = new DropOption[0];

    private float currentHP;
    private bool isDestroyed;
    private TreasureBoxSpawnManager spawnManager;

    public event Action<TreasureBoxController> Destroyed;

    public float MaxHP
    {
        get => maxHP;
        set => maxHP = Mathf.Max(1f, value);
    }

    private void Awake()
    {
        currentHP = MaxHP;
        AssignEnemyLayerIfAvailable();
        EnsureCombatPhysicsSetup();
    }

    private void OnValidate()
    {
        maxHP = Mathf.Max(1f, maxHP);
    }

    private void OnDestroy()
    {
        Destroyed?.Invoke(this);

        if (spawnManager != null)
        {
            spawnManager.UnregisterTreasureBox(this);
        }
    }

    public void Initialize(TreasureBoxSpawnManager owner)
    {
        spawnManager = owner;
        currentHP = MaxHP;
        isDestroyed = false;
    }

    public void TakeDamage(float damage)
    {
        TakeDamage(damage, null);
    }

    public float TakeDamage(float damage, PlayerStatus attackerStatus)
    {
        if (GameplayPauseManager.IsPaused || damage <= 0f || isDestroyed)
        {
            return 0f;
        }

        float previousHP = currentHP;
        currentHP = Mathf.Max(0f, currentHP - damage);
        float appliedDamage = previousHP - currentHP;

        if (appliedDamage > 0f)
        {
            DamageConfirmedEvent.RaisePlayerDamageDealt(attackerStatus, appliedDamage);
        }

        if (currentHP <= 0f)
        {
            Break(attackerStatus);
        }

        return appliedDamage;
    }

    private void Break(PlayerStatus attackerStatus)
    {
        if (isDestroyed)
        {
            return;
        }

        isDestroyed = true;
        Vector3 breakPosition = transform.position;
        TryDropRewards(breakPosition);
        MonsterDeathEvent.RaisePlayerKilledMonster(attackerStatus, breakPosition);
        Destroy(gameObject);
    }

    private void TryDropRewards(Vector3 dropPosition)
    {
        if (dropOptions == null)
        {
            return;
        }

        for (int i = 0; i < dropOptions.Length; i++)
        {
            DropOption dropOption = dropOptions[i];
            if (dropOption == null || dropOption.DropPrefab == null)
            {
                continue;
            }

            float chance = Mathf.Clamp01(dropOption.Chance);
            if (UnityEngine.Random.value > chance)
            {
                continue;
            }

            Instantiate(dropOption.DropPrefab, dropPosition, Quaternion.identity);
        }
    }

    private void AssignEnemyLayerIfAvailable()
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer >= 0)
        {
            gameObject.layer = enemyLayer;
        }
    }

    private void EnsureCombatPhysicsSetup()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.freezeRotation = true;

        Collider2D hitCollider = GetComponent<Collider2D>();
        if (hitCollider == null)
        {
            hitCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        hitCollider.isTrigger = true;
    }

    [Serializable]
    public sealed class DropOption
    {
        [SerializeField] private GameObject dropPrefab;
        [Range(0f, 1f)]
        [SerializeField] private float chance = 1f;

        public GameObject DropPrefab => dropPrefab;
        public float Chance => chance;
    }
}
