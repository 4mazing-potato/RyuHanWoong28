using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ProjectileController : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";
    private const string PlayerTag = "Player";

    [FormerlySerializedAs("damage")]
    [FormerlySerializedAs("projectileDamage")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float scale = 1f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector2 moveDirection = Vector2.right;
    private bool hasHit;
    private PlayerStatus ownerStatus;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        rb.gravityScale = 0f;
    }

    private void Start()
    {
        ApplyMovement();
        Destroy(gameObject, lifetime);
    }

    public void Initialize(Vector2 direction, PlayerStatus ownerStatus, float projectileDamageMultiplier, float projectileSpeed, float projectileLifetime, float projectileScale)
    {
        moveDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        this.ownerStatus = ownerStatus;
        damageMultiplier = Mathf.Max(0f, projectileDamageMultiplier);
        speed = Mathf.Max(0f, projectileSpeed);
        lifetime = Mathf.Max(0.01f, projectileLifetime);
        scale = Mathf.Max(0.01f, projectileScale);

        transform.localScale = Vector3.one * scale;
        ApplyRotation();
        ApplyMovement();

        CancelInvoke();
        Destroy(gameObject, lifetime);
    }

    private void ApplyMovement()
    {
        if (rb == null)
        {
            return;
        }

        rb.gravityScale = 0f;
        rb.linearVelocity = moveDirection * speed;
    }

    private void ApplyRotation()
    {
        bool isLeftOnly = moveDirection.x < 0f && Mathf.Approximately(moveDirection.y, 0f);

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = isLeftOnly;
        }

        float angle = isLeftOnly ? 0f : Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamageEnemy(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamageEnemy(collision.gameObject);
    }

    private void TryDamageEnemy(GameObject target)
    {
        if (GameplayPauseManager.IsPaused || hasHit || ShouldIgnoreTarget(target) || !IsEnemy(target))
        {
            return;
        }

        hasHit = true;

        float damage = CalculateDamage();
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage, ownerStatus);
        }
        else
        {
            target.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
        }

        Destroy(gameObject);
    }

    private float CalculateDamage()
    {
        return ownerStatus != null ? ownerStatus.CalculateDamage(damageMultiplier) : damageMultiplier;
    }

    private bool ShouldIgnoreTarget(GameObject target)
    {
        return target == null
            || target == gameObject
            || target.CompareTag(PlayerTag)
            || target.GetComponentInParent<ProjectileController>() != null;
    }

    private static bool IsEnemy(GameObject target)
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        return enemyLayer >= 0 && target.layer == enemyLayer;
    }
}
