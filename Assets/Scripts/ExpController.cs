using UnityEngine;

public class ExpController : MonoBehaviour
{
    [SerializeField] private int expValue = 1;

    private bool isAbsorbed;

    public int ExpValue => Mathf.Max(0, expValue);

    private void Awake()
    {
        EnsureOrbComponents();
    }

    private void Update()
    {
        if (isAbsorbed)
        {
            return;
        }

        ExpDropManager dropManager = ExpDropManager.Instance;
        if (dropManager == null || dropManager.PlayerTarget == null)
        {
            return;
        }

        Vector3 playerPosition = dropManager.PlayerTarget.position;
        playerPosition.z = transform.position.z;

        float magnetRange = dropManager.MagnetRange;
        if ((playerPosition - transform.position).sqrMagnitude > magnetRange * magnetRange)
        {
            return;
        }

        Absorb(dropManager);
    }

    private void Absorb(ExpDropManager dropManager)
    {
        isAbsorbed = true;
        dropManager.AddExp(ExpValue);
        Destroy(gameObject);
    }

    private void EnsureOrbComponents()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        Collider2D orbCollider = GetComponent<Collider2D>();
        if (orbCollider == null)
        {
            orbCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        orbCollider.isTrigger = true;
    }
}
