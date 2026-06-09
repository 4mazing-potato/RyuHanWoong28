using UnityEngine;

public class CoinController : MagentCollectible
{
    [Header("Coin Reward")]
    [SerializeField] private int minCoin = 1;
    [SerializeField] private int maxCoin = 10;

    public int MinCoin => Mathf.Max(0, minCoin);
    public int MaxCoin => Mathf.Max(MinCoin, maxCoin);

    private void OnValidate()
    {
        minCoin = Mathf.Max(0, minCoin);
        maxCoin = Mathf.Max(minCoin, maxCoin);
    }

    protected override void OnCollected(Transform collector)
    {
        CoinManager coinManager = CoinManager.Instance;
        if (coinManager != null)
        {
            coinManager.AddCoins(GetRandomCoinAmount());
        }

        Destroy(gameObject);
    }

    private int GetRandomCoinAmount()
    {
        int min = MinCoin;
        int max = MaxCoin;
        return min == max ? min : Random.Range(min, max + 1);
    }
}
