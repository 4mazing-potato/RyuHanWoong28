using System.Collections.Generic;
using UnityEngine;

public class FireGroundArea : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";

    private readonly HashSet<int> damagedTargetsThisTick = new HashSet<int>();

    private PlayerStatus ownerStatus;
    private LayerMask affectsLayers;
    private float damage;
    private float duration;
    private float diameter;
    private float despawnTime;

    public void Initialize(PlayerStatus ownerStatus, float damage, float diameter, float duration, LayerMask affectsLayers)
    {
        this.ownerStatus = ownerStatus;
        this.damage = Mathf.Max(0f, damage);
        this.diameter = Mathf.Max(0f, diameter);
        this.duration = Mathf.Max(0.01f, duration);
        this.affectsLayers = affectsLayers.value != 0 ? affectsLayers : GetDefaultEnemyLayerMask();
        despawnTime = Time.time + this.duration;

        ConfigurePhysics();
        ApplyDiameter();
        Destroy(gameObject, this.duration);
    }

    private void FixedUpdate()
    {
        if (GameplayPauseManager.IsPaused)
        {
            return;
        }

        if (Time.time >= despawnTime)
        {
            Destroy(gameObject);
            return;
        }

        DamageEnemiesInArea();
    }

    private void DamageEnemiesInArea()
    {
        if (damage <= 0f || diameter <= 0f)
        {
            return;
        }

        damagedTargetsThisTick.Clear();
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, diameter * 0.5f, affectsLayers);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider2D hitCollider = hitColliders[i];
            if (hitCollider == null)
            {
                continue;
            }

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                continue;
            }

            int targetKey = GetDamageableKey(hitCollider, damageable);
            if (!damagedTargetsThisTick.Add(targetKey))
            {
                continue;
            }

            damageable.TakeDamage(damage, ownerStatus);
        }
    }

    private void ConfigurePhysics()
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

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        if (colliders.Length == 0)
        {
            CircleCollider2D circleCollider = gameObject.AddComponent<CircleCollider2D>();
            circleCollider.isTrigger = true;
            circleCollider.radius = 0.5f;
            return;
        }

        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].isTrigger = true;
        }
    }

    private void ApplyDiameter()
    {
        float clampedDiameter = Mathf.Max(0.01f, diameter);
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            transform.localScale = new Vector3(clampedDiameter, clampedDiameter, 1f);
            return;
        }

        float currentVisualDiameter = Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        if (currentVisualDiameter <= 0f)
        {
            transform.localScale = new Vector3(clampedDiameter, clampedDiameter, 1f);
            return;
        }

        float scaleMultiplier = clampedDiameter / currentVisualDiameter;
        transform.localScale = new Vector3(
            transform.localScale.x * scaleMultiplier,
            transform.localScale.y * scaleMultiplier,
            transform.localScale.z);
    }

    private static int GetDamageableKey(Collider2D hitCollider, IDamageable damageable)
    {
        Component damageableComponent = damageable as Component;
        if (damageableComponent != null)
        {
            return damageableComponent.GetInstanceID();
        }

        Rigidbody2D attachedRigidbody = hitCollider.attachedRigidbody;
        return attachedRigidbody != null ? attachedRigidbody.GetInstanceID() : hitCollider.GetInstanceID();
    }

    private static LayerMask GetDefaultEnemyLayerMask()
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        return enemyLayer >= 0 ? (LayerMask)(1 << enemyLayer) : default;
    }
}
