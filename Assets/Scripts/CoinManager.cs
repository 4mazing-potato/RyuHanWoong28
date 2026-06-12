using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string CoinTextObjectName = "Txt_Coin";
    private const string CoinTextFormat = "Coin : {0}";
    private const string TotalCoinSaveKey = "TotalCoin";

    public static CoinManager Instance { get; private set; }

    [Header("Coin")]
    [SerializeField] private int currentCoin;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI coinText;

    public int CurrentCoin => currentCoin;

    public static int TotalCoin => LoadTotalCoins();

    public static int LoadTotalCoins()
    {
        return Mathf.Max(0, PlayerPrefs.GetInt(TotalCoinSaveKey, 0));
    }

    public static void SaveTotalCoins(int totalCoin)
    {
        PlayerPrefs.SetInt(TotalCoinSaveKey, Mathf.Max(0, totalCoin));
        PlayerPrefs.Save();
    }

    public static int AddToTotalCoins(int coinAmount)
    {
        if (coinAmount <= 0)
        {
            return LoadTotalCoins();
        }

        int totalCoin = LoadTotalCoins() + coinAmount;
        SaveTotalCoins(totalCoin);
        return totalCoin;
    }

    public static void ResetTotalCoins()
    {
        SaveTotalCoins(0);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForGameScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName || FindObjectOfType<CoinManager>() != null)
        {
            return;
        }

        new GameObject(nameof(CoinManager)).AddComponent<CoinManager>();
    }

    private void OnValidate()
    {
        currentCoin = Mathf.Max(0, currentCoin);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        currentCoin = Mathf.Max(0, currentCoin);
        CacheUiReferences();
        UpdateCoinText();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (coinText == null)
        {
            CacheUiReferences();
            UpdateCoinText();
        }
    }

    public void AddCoins(int coinAmount)
    {
        if (coinAmount <= 0)
        {
            return;
        }

        currentCoin += coinAmount;
        CacheUiReferences();
        UpdateCoinText();
    }

    private void CacheUiReferences()
    {
        if (coinText != null)
        {
            return;
        }

        GameObject coinTextObject = GameObject.Find(CoinTextObjectName);
        if (coinTextObject != null)
        {
            coinText = coinTextObject.GetComponent<TextMeshProUGUI>();
        }
    }

    private void UpdateCoinText()
    {
        if (coinText != null)
        {
            coinText.text = string.Format(CoinTextFormat, currentCoin);
        }
    }
}
