using UnityEngine;

public interface IMagentCollectible
{
    bool IsCollected { get; }
    void MoveTowardCollector(Transform collector, float deltaTime);
    void Collect(Transform collector);
}

public abstract class MagentCollectible : MonoBehaviour, IMagentCollectible
{
    private const string CollectibleTag = "MagentCollectible";

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

        ExpDropManager dropManager = ExpDropManager.Instance;
        if (dropManager == null || dropManager.PlayerTarget == null)
        {
            return;
        }

        Transform collector = dropManager.PlayerTarget;
        Vector3 collectorPosition = collector.position;
        collectorPosition.z = transform.position.z;

        float pickupRadius = dropManager.PickupRadius;
        if ((collectorPosition - transform.position).sqrMagnitude > pickupRadius * pickupRadius)
        {
            return;
        }

        MoveTowardCollector(collector, Time.deltaTime);

        if ((collectorPosition - transform.position).sqrMagnitude <= CollectDistance * CollectDistance)
        {
            Collect(collector);
        }
    }

    public virtual void MoveTowardCollector(Transform collector, float deltaTime)
    {
        if (collector == null)
        {
            return;
        }

        Vector3 collectorPosition = collector.position;
        collectorPosition.z = transform.position.z;
        transform.position = Vector3.MoveTowards(transform.position, collectorPosition, MoveSpeed * Mathf.Max(0f, deltaTime));
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
