using UnityEngine;
using UnityEngine.SceneManagement;

public class ExpDropManager : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string PlayerTag = "Player";

    public static ExpDropManager Instance { get; private set; }

    [SerializeField] private float magnetRange = 2.5f;
    [SerializeField] private Transform playerTarget;
    [SerializeField] private PlayerExperience playerExperience;
    [SerializeField] private int totalExp;

    public float MagnetRange => Mathf.Max(0f, magnetRange);
    public Transform PlayerTarget => playerTarget;
    public PlayerExperience PlayerExperience => playerExperience;
    public int TotalExp => totalExp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForGameScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName || FindObjectOfType<ExpDropManager>() != null)
        {
            return;
        }

        new GameObject(nameof(ExpDropManager)).AddComponent<ExpDropManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        FindPlayerTarget();
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
        if (playerTarget == null || playerExperience == null)
        {
            FindPlayerTarget();
        }
    }

    public void AddExp(int expAmount)
    {
        if (GameplayPauseManager.IsPaused || expAmount <= 0)
        {
            return;
        }

        totalExp += expAmount;

        if (playerExperience == null)
        {
            FindPlayerTarget();
        }

        if (playerExperience != null)
        {
            playerExperience.AddExp(expAmount);
        }
    }

    private void FindPlayerTarget()
    {
        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        playerTarget = player != null ? player.transform : null;
        playerExperience = player != null ? player.GetComponent<PlayerExperience>() : null;
    }
}
