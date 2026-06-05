using UnityEngine;

public class SuperMagnetController : MagentCollectible
{
    protected override void OnCollected(Transform collector)
    {
        MagnetBoostController magnetBoost = collector != null ? collector.GetComponent<MagnetBoostController>() : null;
        if (magnetBoost == null && collector != null)
        {
            magnetBoost = collector.GetComponentInParent<MagnetBoostController>();
        }

        if (magnetBoost != null)
        {
            magnetBoost.MagnetBoost();
        }

        Destroy(gameObject);
    }
}
