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
    private ProjectileSplitController splitController;
    private SplashDamageController splashDamageController;
    private bool canSplit = true;

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
        Initialize(direction, ownerStatus, projectileDamageMultiplier, projectileSpeed, projectileLifetime, projectileScale, true);
    }

    public void Initialize(Vector2 direction, PlayerStatus ownerStatus, float projectileDamageMultiplier, float projectileSpeed, float projectileLifetime, float projectileScale, bool canSplit)
    {
        moveDirection = direction.sqrMagnitude > 0f ? direction.normalized : Vector2.right;
        this.ownerStatus = ownerStatus;
        this.canSplit = canSplit;
        hasHit = false;
        damageMultiplier = Mathf.Max(0f, projectileDamageMultiplier);
        speed = Mathf.Max(0f, projectileSpeed);
        lifetime = Mathf.Max(0.01f, projectileLifetime);
        scale = Mathf.Max(0.01f, projectileScale);
        CacheOwnerSkillControllers();

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
        Vector3 hitPosition = other != null ? (Vector3)other.ClosestPoint(transform.position) : transform.position;
        TryDamageEnemy(other != null ? other.gameObject : null, other, hitPosition);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        Vector3 hitPosition = collision != null && collision.contactCount > 0 ? (Vector3)collision.GetContact(0).point : transform.position;
        TryDamageEnemy(collision != null ? collision.gameObject : null, collision != null ? collision.collider : null, hitPosition);
    }

    private void TryDamageEnemy(GameObject target, Collider2D hitCollider, Vector3 hitPosition)
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

        TryApplySplashDamage(hitPosition, damage, hitCollider, damageable);
        TrySplitProjectile(hitPosition);

        Destroy(gameObject);
    }

    private float CalculateDamage()
    {
        return ownerStatus != null ? ownerStatus.CalculateDamage(damageMultiplier) : damageMultiplier;
    }

    private void TryApplySplashDamage(Vector3 hitPosition, float projectileDamage, Collider2D directHitCollider, IDamageable directHitDamageable)
    {
        CacheOwnerSkillControllers();
        if (splashDamageController != null)
        {
            splashDamageController.ApplySplashDamage(hitPosition, projectileDamage, directHitCollider, directHitDamageable);
        }
    }

    private void TrySplitProjectile(Vector3 hitPosition)
    {
        CacheOwnerSkillControllers();
        if (canSplit && splitController != null)
        {
            splitController.SplitProjectile(this, hitPosition, ownerStatus, damageMultiplier, speed, lifetime, scale);
        }
    }

    private void CacheOwnerSkillControllers()
    {
        if (ownerStatus == null)
        {
            return;
        }

        if (splitController == null)
        {
            splitController = ownerStatus.GetComponent<ProjectileSplitController>();
        }

        if (splashDamageController == null)
        {
            splashDamageController = ownerStatus.GetComponent<SplashDamageController>();
        }
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
