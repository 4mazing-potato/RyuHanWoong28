using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float scale = 1f;

    private Rigidbody2D rb;
    private Vector2 moveDirection = Vector2.right;
    private float destroyAt;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
    }

    private void OnEnable()
    {
        destroyAt = Time.time + lifetime;
    }

    private void Update()
    {
        if (Time.time >= destroyAt)
        {
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = moveDirection * speed;
    }

    public void Initialize(Vector2 direction, float projectileDamage, float projectileSpeed, float projectileLifetime, float projectileScale)
    {
        if (direction.sqrMagnitude > 0f)
        {
            moveDirection = direction.normalized;
        }

        damage = projectileDamage;
        speed = projectileSpeed;
        lifetime = projectileLifetime;
        scale = projectileScale;
        destroyAt = Time.time + lifetime;

        transform.localScale = Vector3.one * scale;
        ApplyVisualDirection();
    }

    private void ApplyVisualDirection()
    {
        float angle = Mathf.Atan2(moveDirection.y, Mathf.Abs(moveDirection.x)) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = moveDirection.x < 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        HandleEnemyHit(other.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleEnemyHit(collision.gameObject);
    }

    private void HandleEnemyHit(GameObject target)
    {
        if (target.layer != LayerMask.NameToLayer("Enemy"))
        {
            return;
        }

        DealDamage(target);
        Destroy(gameObject);
    }

    private void DealDamage(GameObject target)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
            return;
        }

        target.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
    }
}
