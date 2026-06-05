using UnityEngine;

public interface IMagentCollectible
{
    bool IsCollected { get; }
    void MoveTowardCollector(Transform collector, float deltaTime);
    void Collect(Transform collector);
}

public abstract class MagentCollectible : MonoBehaviour, IMagentCollectible
{
    private const string CollectibleTag = "MagnetCollectible";

    [Header("Magnet Collectible")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float collectDistance = 0.15f;

    private bool isCollected;

    public bool IsCollected => isCollected;
    public float MoveSpeed => Mathf.Max(0f, moveSpeed);
    public float CollectDistance => Mathf.Max(0f, collectDistance);

    protected virtual void Awake()
    {
        EnsureCollectibleSetup();
    }

    protected virtual void Update()
    {
        if (GameplayPauseManager.IsPaused || isCollected)
        {
            return;
        }

        MagnetBoostController magnetBoost = MagnetBoostController.ActiveInstance;
        bool isBoosted = magnetBoost != null && magnetBoost.IsBoostActive && magnetBoost.Collector != null;
        Transform collector = isBoosted ? magnetBoost.Collector : null;
        float speedMultiplier = isBoosted ? magnetBoost.BoostSpeedMultiplier : 1f;

        ExpDropManager dropManager = ExpDropManager.Instance;
        if (!isBoosted)
        {
            if (dropManager == null || dropManager.PlayerTarget == null)
            {
                return;
            }

            collector = dropManager.PlayerTarget;
        }

        Vector3 collectorPosition = collector.position;
        collectorPosition.z = transform.position.z;

        if (!isBoosted)
        {
            float pickupRadius = dropManager.PickupRadius;
            if ((collectorPosition - transform.position).sqrMagnitude > pickupRadius * pickupRadius)
            {
                return;
            }
        }

        MoveTowardCollector(collector, Time.deltaTime, speedMultiplier);

        if ((collectorPosition - transform.position).sqrMagnitude <= CollectDistance * CollectDistance)
        {
            Collect(collector);
        }
    }

    public virtual void MoveTowardCollector(Transform collector, float deltaTime)
    {
        MoveTowardCollector(collector, deltaTime, 1f);
    }

    public virtual void MoveTowardCollector(Transform collector, float deltaTime, float speedMultiplier)
    {
        if (collector == null)
        {
            return;
        }

        Vector3 collectorPosition = collector.position;
        collectorPosition.z = transform.position.z;
        float boostedSpeed = MoveSpeed * Mathf.Max(0f, speedMultiplier);
        transform.position = Vector3.MoveTowards(transform.position, collectorPosition, boostedSpeed * Mathf.Max(0f, deltaTime));
    }

    public void Collect(Transform collector)
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;
        OnCollected(collector);
    }

    protected abstract void OnCollected(Transform collector);

    private void EnsureCollectibleSetup()
    {
        if (gameObject.tag != CollectibleTag)
        {
            gameObject.tag = CollectibleTag;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Collider2D collectibleCollider = GetComponent<Collider2D>();
        if (collectibleCollider == null)
        {
            collectibleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        collectibleCollider.isTrigger = true;
    }
}
