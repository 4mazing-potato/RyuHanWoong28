using UnityEngine;

[DisallowMultipleComponent]
public class MagnetBoostController : MonoBehaviour
{
    private const string CollectibleTag = "MagnetCollectible";

    public static MagnetBoostController ActiveInstance { get; private set; }

    [Header("Magnet Boost")]
    [SerializeField] private float boostDuration = 5f;
    [SerializeField] private float boostSpeedMultiplier = 3f;

    private float remainingBoostTime;

    public bool IsBoostActive => remainingBoostTime > 0f;
    public float BoostDuration => Mathf.Max(0f, boostDuration);
    public float BoostSpeedMultiplier => Mathf.Max(1f, boostSpeedMultiplier);
    public Transform Collector => transform;

    private void Awake()
    {
        ActiveInstance = this;
    }

    private void OnDestroy()
    {
        if (ActiveInstance == this)
        {
            ActiveInstance = null;
        }
    }

    private void Update()
    {
        if (!IsBoostActive || GameplayPauseManager.IsPaused)
        {
            return;
        }

        remainingBoostTime -= Time.deltaTime;
        if (remainingBoostTime <= 0f)
        {
            remainingBoostTime = 0f;
        }
    }

    public void MagnetBoost()
    {
        remainingBoostTime = BoostDuration;
        PullExistingCollectibles();
    }

    private void PullExistingCollectibles()
    {
        GameObject[] collectibles;
        try
        {
            collectibles = GameObject.FindGameObjectsWithTag(CollectibleTag);
        }
        catch (UnityException)
        {
            return;
        }

        float deltaTime = Time.deltaTime > 0f ? Time.deltaTime : Time.fixedDeltaTime;
        for (int i = 0; i < collectibles.Length; i++)
        {
            MagentCollectible collectible = collectibles[i] != null ? collectibles[i].GetComponent<MagentCollectible>() : null;
            if (collectible == null || collectible.IsCollected)
            {
                continue;
            }

            collectible.MoveTowardCollector(Collector, deltaTime, BoostSpeedMultiplier);
        }
    }
}
