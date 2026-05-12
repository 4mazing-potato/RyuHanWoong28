using UnityEngine;

public class AutoShooter : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float speed = 8f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float scale = 1f;

    [Header("Fire Timing")]
    [SerializeField] private float fireInterval = 0.5f;
    [SerializeField] private float minTurnCooldown = 0.15f;

    private Vector2 fireDirection = Vector2.right;
    private bool hasFireDirection;
    private float nextFireTime;
    private float turnCooldownUntil;

    private void Awake()
    {
        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    private void Update()
    {
        UpdateFireDirection();
        TryFire();
    }

    private void UpdateFireDirection()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector2 newDirection = input.normalized;
        if (!hasFireDirection)
        {
            fireDirection = newDirection;
            hasFireDirection = true;
            return;
        }

        if (Vector2.Dot(fireDirection, newDirection) >= 0.999f)
        {
            fireDirection = newDirection;
            return;
        }

        fireDirection = newDirection;
        turnCooldownUntil = Time.time + minTurnCooldown;
    }

    private void TryFire()
    {
        if (!hasFireDirection || projectilePrefab == null || Time.time < nextFireTime || Time.time < turnCooldownUntil)
        {
            return;
        }

        Projectile projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
        projectile.Initialize(fireDirection, damage, speed, lifetime, scale);

        nextFireTime = Time.time + fireInterval;
    }
}
