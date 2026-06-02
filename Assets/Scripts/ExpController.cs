using UnityEngine;

public class ExpController : MagentCollectible
{
    [SerializeField] private int expValue = 1;

    public int ExpValue => Mathf.Max(0, expValue);

    protected override void OnCollected(Transform collector)
    {
        ExpDropManager dropManager = ExpDropManager.Instance;
        if (dropManager != null)
        {
            dropManager.AddExp(ExpValue);
        }

        Destroy(gameObject);
    }
}
