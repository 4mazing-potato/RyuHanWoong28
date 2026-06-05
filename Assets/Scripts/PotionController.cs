using UnityEngine;

public class PotionController : MagentCollectible
{
    [Header("Potion")]
    [SerializeField] private float healValue = 10f;

    public float HealValue => Mathf.Max(0f, healValue);

    private void OnValidate()
    {
        healValue = Mathf.Max(0f, healValue);
    }

    protected override void OnCollected(Transform collector)
    {
        PlayerHealth playerHealth = collector != null ? collector.GetComponent<PlayerHealth>() : null;
        if (playerHealth == null && collector != null)
        {
            playerHealth = collector.GetComponentInParent<PlayerHealth>();
        }

        if (playerHealth != null)
        {
            playerHealth.Heal(HealValue);
        }

        Destroy(gameObject);
    }
}
