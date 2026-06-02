using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class LevelUpSkillUpgradeController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string CardContainerName = "CardContainer";
    private const string CardOneName = "Card_1";
    private const string CardTwoName = "Card_2";
    private const string CardThreeName = "Card_3";
    private const string IconObjectName = "Image_Icon";
    private const string LegacyIconObjectName = "Image Icon";
    private const string DescriptionObjectName = "Text_Description";
    private const string LegacyDescriptionObjectName = "Text_Destcription";
    private const string SpriteFolderPath = "Assets/Sprites";
    private const string PlayerTag = "Player";
    private const int MaxOfferedCardCount = 3;

    public static LevelUpSkillUpgradeController Instance { get; private set; }

    [SerializeField] private GameObject cardContainer;
    [SerializeField] private Button cardOneButton;
    [SerializeField] private Button cardTwoButton;
    [SerializeField] private Button cardThreeButton;

    private readonly HashSet<int> selectedCardIds = new HashSet<int>();
    private readonly LevelUpCardData[] offeredCards = new LevelUpCardData[MaxOfferedCardCount];
    private readonly bool[] hasOfferedCard = new bool[MaxOfferedCardCount];

    private int pendingLevelUpCount;
    private bool isPanelOpen;
    private AutoShooter playerShooter;
    private PlayerStatus playerStatus;
    private PlayerHealth playerHealth;

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

        int offeredCardCount = BuildOfferedCards();
        if (offeredCardCount <= 0)
        {
            Debug.LogWarning("There are no selectable level-up cards left in LevelUpCard.csv.", this);
            TryOpenNextPanel();
            return;
        }

        RefreshCardViews();
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

        int cardSlot = cardIndex - 1;
        if (cardSlot < 0 || cardSlot >= MaxOfferedCardCount || !hasOfferedCard[cardSlot])
        {
            return;
        }

        LevelUpCardData selectedCard = offeredCards[cardSlot];
        selectedCardIds.Add(selectedCard.ID);
        ApplyUpgrade(selectedCard);
        ClosePanelAndResume();
        TryOpenNextPanel();
    }

    private int BuildOfferedCards()
    {
        ClearOfferedCards();
        List<LevelUpCardData> candidates = BuildCandidateCards();
        int drawCount = Mathf.Min(MaxOfferedCardCount, candidates.Count);

        for (int i = 0; i < drawCount; i++)
        {
            int selectedIndex = PickWeightedCandidateIndex(candidates);
            offeredCards[i] = candidates[selectedIndex];
            hasOfferedCard[i] = true;
            candidates.RemoveAt(selectedIndex);
        }

        return drawCount;
    }

    private List<LevelUpCardData> BuildCandidateCards()
    {
        IReadOnlyList<LevelUpCardData> rows = LevelUpCardTable.Rows;
        List<LevelUpCardData> candidates = new List<LevelUpCardData>();

        for (int i = 0; i < rows.Count; i++)
        {
            LevelUpCardData row = rows[i];
            if (selectedCardIds.Contains(row.ID))
            {
                continue;
            }

            if (row.Required.HasValue && !selectedCardIds.Contains(row.Required.Value))
            {
                continue;
            }

            candidates.Add(row);
        }

        return candidates;
    }

    private static int PickWeightedCandidateIndex(IReadOnlyList<LevelUpCardData> candidates)
    {
        float totalRatio = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            totalRatio += Mathf.Max(0f, candidates[i].Ratio);
        }

        if (totalRatio <= 0f)
        {
            return Random.Range(0, candidates.Count);
        }

        float roll = Random.Range(0f, totalRatio);
        for (int i = 0; i < candidates.Count; i++)
        {
            roll -= Mathf.Max(0f, candidates[i].Ratio);
            if (roll <= 0f)
            {
                return i;
            }
        }

        return candidates.Count - 1;
    }

    private void ClearOfferedCards()
    {
        for (int i = 0; i < MaxOfferedCardCount; i++)
        {
            offeredCards[i] = default;
            hasOfferedCard[i] = false;
        }
    }

    private void RefreshCardViews()
    {
        RefreshCardView(cardOneButton, 0);
        RefreshCardView(cardTwoButton, 1);
        RefreshCardView(cardThreeButton, 2);
    }

    private void RefreshCardView(Button button, int cardSlot)
    {
        if (button == null)
        {
            return;
        }

        bool hasCard = hasOfferedCard[cardSlot];
        button.gameObject.SetActive(hasCard);
        button.interactable = hasCard;
        if (!hasCard)
        {
            return;
        }

        LevelUpCardData card = offeredCards[cardSlot];
        Image iconImage = FindChildComponentByName<Image>(button.transform, IconObjectName);
        if (iconImage == null)
        {
            iconImage = FindChildComponentByName<Image>(button.transform, LegacyIconObjectName);
        }

        if (iconImage != null)
        {
            iconImage.sprite = LoadCardIcon(card.Icon);
            iconImage.enabled = iconImage.sprite != null;
        }

        TextMeshProUGUI descriptionText = FindChildComponentByName<TextMeshProUGUI>(button.transform, DescriptionObjectName);
        if (descriptionText == null)
        {
            descriptionText = FindChildComponentByName<TextMeshProUGUI>(button.transform, LegacyDescriptionObjectName);
        }

        if (descriptionText != null)
        {
            descriptionText.text = card.Desc;
        }
    }

    private void ApplyUpgrade(LevelUpCardData card)
    {
        float value = card.Value.GetValueOrDefault();
        switch (card.Effect)
        {
            case LevelUpCardEffect.ATKUP:
                PlayerStatus status = GetPlayerStatus();
                if (status != null)
                {
                    status.ApplyAttackUpPercent(value);
                }

                break;
            case LevelUpCardEffect.HPUp:
                PlayerStatus hpStatus = GetPlayerStatus();
                if (hpStatus != null)
                {
                    hpStatus.ApplyHPUpPercent(value);
                    PlayerHealth hpHealth = GetPlayerHealth();
                    if (hpHealth != null)
                    {
                        hpHealth.RefreshHealthView();
                    }
                }

                break;
            case LevelUpCardEffect.HEAL:
                PlayerStatus healStatus = GetPlayerStatus();
                if (healStatus != null)
                {
                    healStatus.SetHealOnDamagePercent(value);
                    PlayerHealth healHealth = GetPlayerHealth();
                    if (healHealth != null)
                    {
                        healHealth.RefreshHealthView();
                    }
                }

                break;
            case LevelUpCardEffect.IncreaseDamageMultiplier:
                AutoShooter damageShooter = GetPlayerShooter();
                if (damageShooter != null)
                {
                    damageShooter.IncreaseDamageMultiplier(value);
                }

                break;
            case LevelUpCardEffect.MultiplyFireInterval:
                AutoShooter intervalShooter = GetPlayerShooter();
                if (intervalShooter != null)
                {
                    intervalShooter.MultiplyFireInterval(value);
                }

                break;
            case LevelUpCardEffect.IncreaseProjectileScale:
                AutoShooter scaleShooter = GetPlayerShooter();
                if (scaleShooter != null)
                {
                    scaleShooter.IncreaseProjectileScale(value);
                }

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
        ClearOfferedCards();
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

    private static T FindChildComponentByName<T>(Transform root, string childName) where T : Component
    {
        if (root == null)
        {
            return null;
        }

        T rootComponent = root.name == childName ? root.GetComponent<T>() : null;
        if (rootComponent != null)
        {
            return rootComponent;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            T childComponent = FindChildComponentByName<T>(root.GetChild(i), childName);
            if (childComponent != null)
            {
                return childComponent;
            }
        }

        return null;
    }

    private static Sprite LoadCardIcon(string iconName)
    {
        if (string.IsNullOrWhiteSpace(iconName))
        {
            return null;
        }

        Sprite sprite = Resources.Load<Sprite>(iconName);
        if (sprite != null)
        {
            return sprite;
        }

#if UNITY_EDITOR
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolderPath}/{iconName}.png");
#else
        return null;
#endif
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

    private PlayerHealth GetPlayerHealth()
    {
        if (playerHealth != null)
        {
            return playerHealth;
        }

        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        playerHealth = player != null ? player.GetComponent<PlayerHealth>() : null;
        return playerHealth;
    }

    private PlayerStatus GetPlayerStatus()
    {
        if (playerStatus != null)
        {
            return playerStatus;
        }

        GameObject player = GameObject.FindGameObjectWithTag(PlayerTag);
        playerStatus = player != null ? player.GetComponent<PlayerStatus>() : null;
        return playerStatus;
    }
}
