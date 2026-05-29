using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelUpSkillUpgradeController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string CardContainerName = "CardContainer";
    private const string CardOneName = "Card_1";
    private const string CardTwoName = "Card_2";
    private const string CardThreeName = "Card_3";
    private const string PlayerTag = "Player";

    public static LevelUpSkillUpgradeController Instance { get; private set; }

    [SerializeField] private GameObject cardContainer;
    [SerializeField] private Button cardOneButton;
    [SerializeField] private Button cardTwoButton;
    [SerializeField] private Button cardThreeButton;

    [Header("Upgrade Values")]
    [SerializeField] private float damageBonus = 1f;
    [SerializeField] private float fireIntervalMultiplier = 0.85f;
    [SerializeField] private float projectileScaleBonus = 0.15f;

    private int pendingLevelUpCount;
    private bool isPanelOpen;
    private AutoShooter playerShooter;

    public bool IsPanelOpen => isPanelOpen;
    public int PendingLevelUpCount => pendingLevelUpCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForGameScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName || FindObjectOfType<LevelUpSkillUpgradeController>() != null)
        {
            return;
        }

        new GameObject(nameof(LevelUpSkillUpgradeController)).AddComponent<LevelUpSkillUpgradeController>();
    }

    public static LevelUpSkillUpgradeController EnsureInstance()
    {
        if (Instance != null)
        {
            return Instance;
        }

        LevelUpSkillUpgradeController existingController = FindObjectOfType<LevelUpSkillUpgradeController>();
        if (existingController != null)
        {
            return existingController;
        }

        return new GameObject(nameof(LevelUpSkillUpgradeController)).AddComponent<LevelUpSkillUpgradeController>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CacheReferences();
        ConfigureCards();
        ClosePanelWithoutResuming();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (isPanelOpen)
            {
                GameplayPauseManager.ReleasePause();
            }

            Instance = null;
        }
    }

    public void EnqueueLevelUps(int levelUpCount)
    {
        if (levelUpCount <= 0)
        {
            return;
        }

        pendingLevelUpCount += levelUpCount;
        TryOpenNextPanel();
    }

    private void TryOpenNextPanel()
    {
        if (isPanelOpen || pendingLevelUpCount <= 0)
        {
            return;
        }

        pendingLevelUpCount--;
        OpenPanel();
    }

    private void OpenPanel()
    {
        CacheReferences();
        ConfigureCards();

        if (cardContainer == null)
        {
            Debug.LogWarning($"Level-up card UI object '{CardContainerName}' could not be found.", this);
            TryOpenNextPanel();
            return;
        }

        isPanelOpen = true;
        cardContainer.SetActive(true);
        GameplayPauseManager.RequestPause();
    }

    private void SelectCard(int cardIndex)
    {
        if (!isPanelOpen)
        {
            return;
        }

        ApplyUpgrade(cardIndex);
        ClosePanelAndResume();
        TryOpenNextPanel();
    }

    private void ApplyUpgrade(int cardIndex)
    {
        AutoShooter shooter = GetPlayerShooter();
        if (shooter == null)
        {
            return;
        }

        switch (cardIndex)
        {
            case 1:
                shooter.IncreaseDamage(damageBonus);
                break;
            case 2:
                shooter.MultiplyFireInterval(fireIntervalMultiplier);
                break;
            case 3:
                shooter.IncreaseProjectileScale(projectileScaleBonus);
                break;
        }
    }

    private void ClosePanelAndResume()
    {
        ClosePanelWithoutResuming();
        GameplayPauseManager.ReleasePause();
    }

    private void ClosePanelWithoutResuming()
    {
        isPanelOpen = false;
        if (cardContainer != null)
        {
            cardContainer.SetActive(false);
        }
    }

    private void CacheReferences()
    {
        if (cardContainer == null)
        {
            GameObject containerObject = FindSceneObjectByName(CardContainerName);
            if (containerObject != null)
            {
                cardContainer = containerObject;
            }
        }

        cardOneButton = cardOneButton != null ? cardOneButton : FindCardButton(CardOneName);
        cardTwoButton = cardTwoButton != null ? cardTwoButton : FindCardButton(CardTwoName);
        cardThreeButton = cardThreeButton != null ? cardThreeButton : FindCardButton(CardThreeName);
    }

    private void ConfigureCards()
    {
        ConfigureCardButton(cardOneButton, 1);
        ConfigureCardButton(cardTwoButton, 2);
        ConfigureCardButton(cardThreeButton, 3);
    }

    private void ConfigureCardButton(Button button, int cardIndex)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => SelectCard(cardIndex));
    }

    private static Button FindCardButton(string cardName)
    {
        GameObject cardObject = FindSceneObjectByName(cardName);
        return cardObject != null ? cardObject.GetComponent<Button>() : null;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject activeObject = GameObject.Find(objectName);
        if (activeObject != null)
        {
            return activeObject;
        }

        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate.name == objectName && candidate.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }

    private AutoShooter GetPlayerShooter()
    {
        if (playerShooter != null)
        {
            return playerShooter;
        }

        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        playerShooter = player != null ? player.GetComponent<AutoShooter>() : null;
        return playerShooter;
    }
}
