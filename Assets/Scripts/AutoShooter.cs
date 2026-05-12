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

    private Vector2 lastInputDirection = Vector2.right;
    private float nextFireTime;

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

        if (Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + Mathf.Max(0f, fireInterval);
        }
    }

    private void UpdateFireDirection()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (input.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector2 newDirection = input.sqrMagnitude > 1f ? input.normalized : input;
        if (Vector2.Dot(lastInputDirection, newDirection) < 0.999f)
        {
            lastInputDirection = newDirection;
            nextFireTime = Mathf.Max(nextFireTime, Time.time + Mathf.Max(0f, minTurnCooldown));
        }
    }

    private void Fire()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        Transform origin = spawnPoint != null ? spawnPoint : transform;
        Projectile projectile = Instantiate(projectilePrefab, origin.position, Quaternion.identity);
        projectile.Initialize(lastInputDirection, damage, speed, lifetime, scale);
    }
}
