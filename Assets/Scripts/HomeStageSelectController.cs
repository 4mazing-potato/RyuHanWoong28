using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HomeStageSelectController : MonoBehaviour
{
    private const string HomeSceneName = "HomeScene";
    private const string GameSceneName = "GameScene";
    private const string StartButtonName = "BtnStart";
    private const string AlternateStartButtonName = "Btn_Start";
    private const string UpgradeButtonName = "BtnUpgrade";
    private const string AlternateUpgradeButtonName = "Btn_Upgrade";
    private const string ExitButtonName = "BtnExit";
    private const string GameStartPanelName = "Panel_GameStart";
    private const string CloseButtonName = "BtnX";
    private const string AlternateUpgradeCloseButtonName = "BTN_X";
    private const string ContentName = "Content";
    private const string StageButtonName = "BtnStage";
    private const string StageImageName = "StageImage";
    private const string AlternateStageImageName = "Image";
    private const string StageTextName = "Text (TMP)";
    private const string StageSpriteCatalogPath = "StageSpriteCatalog";
    private const string UpgradePanelName = "Panel_Upgrade";
    private const string CoinAmountTextName = "CoinAmount";
    private const string UpgradeCellName = "UpgradeCell";
    private const string StatusTextName = "Txt_Status";
    private const string DotsName = "Dots";
    private const string DotNamePrefix = "Dot_";
    private const string BuyButtonName = "Btn_Buy";
    private const string DotSpriteCatalogResourcePath = "UpgradeDotSpriteCatalog";
    private const int CoinCheatAmount = 100;
    private const float StageButtonSpacing = 12f;

    private Button startButton;
    private Button upgradeButton;
    private Button exitButton;
    private GameObject gameStartPanel;
    private GameObject upgradePanel;
    private Button closeButton;
    private Button upgradeCloseButton;
    private Transform contentRoot;
    private Transform upgradeContentRoot;
    private ScrollRect upgradeScrollRect;
    private GameObject stageButtonTemplate;
    private GameObject upgradeCellTemplate;
    private TMP_Text coinAmountText;
    private Sprite dotInactiveSprite;
    private Sprite dotActiveSprite;
    private readonly List<GameObject> stageButtons = new List<GameObject>();
    private readonly List<GameObject> upgradeCells = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForHomeScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != HomeSceneName || FindObjectOfType<HomeStageSelectController>() != null)
        {
            return;
        }

        GameObject controller = new GameObject(nameof(HomeStageSelectController));
        controller.AddComponent<HomeStageSelectController>();
    }

    private void Awake()
    {
        CacheReferences();
        RegisterButtons();
        PopulateStageButtons();
        PopulateUpgradeCells();
        UpdateTotalCoinText();
        HideGameStartPanel();
        HideUpgradePanel();
    }

    private void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(ShowGameStartPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideGameStartPanel);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(ShowUpgradePanel);
        }

        if (upgradeCloseButton != null)
        {
            upgradeCloseButton.onClick.RemoveListener(HideUpgradePanel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(QuitGame);
        }
    }

    public void ShowGameStartPanel()
    {
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(true);
        }
    }

    public void ShowUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(true);
        }

        PopulateUpgradeCells();
        UpdateTotalCoinText();
    }

    [ContextMenu("Coin/Reset Total Coin To 0")]
    public void ResetTotalCoinCheat()
    {
        CoinManager.ResetTotalCoins();
        UpdateTotalCoinText();
        RefreshUpgradeCells();
    }

    [ContextMenu("Coin/Add 100 Total Coins")]
    public void AddTotalCoinCheat()
    {
        CoinManager.AddToTotalCoins(CoinCheatAmount);
        UpdateTotalCoinText();
        RefreshUpgradeCells();
    }

    [ContextMenu("Upgrade/Reset All Upgrade Levels To 0")]
    public void ResetAllUpgradeLevelsCheat()
    {
        PermanentUpgradeManager.ResetAllUpgradeLevels();
        RefreshUpgradeCells();
    }

    public void HideGameStartPanel()
    {
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(false);
        }
    }

    public void HideUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.SetActive(false);
        }
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CacheReferences()
    {
        startButton = FindButton(StartButtonName);
        if (startButton == null)
        {
            startButton = FindButton(AlternateStartButtonName);
        }

        upgradeButton = FindButton(UpgradeButtonName) ?? FindButton(AlternateUpgradeButtonName);
        exitButton = FindButton(ExitButtonName);
        gameStartPanel = FindSceneGameObject(GameStartPanelName);
        upgradePanel = FindSceneGameObject(UpgradePanelName);
        closeButton = FindButtonInGameStartPanel(CloseButtonName) ?? FindButton(CloseButtonName);
        upgradeCloseButton = FindButtonInUpgradePanel(AlternateUpgradeCloseButtonName) ?? FindButtonInUpgradePanel(CloseButtonName);
        GameObject contentObject = FindGameStartPanelChild(ContentName) ?? FindSceneGameObject(ContentName);
        contentRoot = contentObject != null ? contentObject.transform : null;
        stageButtonTemplate = FindStageButtonTemplate();
        GameObject upgradeContentObject = FindUpgradePanelChild(ContentName);
        upgradeContentRoot = upgradeContentObject != null ? upgradeContentObject.transform : null;
        upgradeScrollRect = FindUpgradeScrollRect();
        ConfigureUpgradeScrollRect();
        upgradeCellTemplate = FindUpgradeCellTemplate();
        CacheDotSprites();
        coinAmountText = FindCoinAmountText();
    }

    private void UpdateTotalCoinText()
    {
        if (coinAmountText == null)
        {
            coinAmountText = FindCoinAmountText();
        }

        if (coinAmountText != null)
        {
            coinAmountText.text = CoinManager.LoadTotalCoins().ToString();
        }
    }

    private TMP_Text FindCoinAmountText()
    {
        Transform upgradePanelTransform = upgradePanel != null ? upgradePanel.transform : null;
        return FindChildComponent<TMP_Text>(upgradePanelTransform, CoinAmountTextName)
            ?? FindChildComponent<TMP_Text>(transform, CoinAmountTextName);
    }

    private void RegisterButtons()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(ShowGameStartPanel);
            startButton.onClick.AddListener(ShowGameStartPanel);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HideGameStartPanel);
            closeButton.onClick.AddListener(HideGameStartPanel);
        }

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(ShowUpgradePanel);
            upgradeButton.onClick.AddListener(ShowUpgradePanel);
        }

        if (upgradeCloseButton != null)
        {
            upgradeCloseButton.onClick.RemoveListener(HideUpgradePanel);
            upgradeCloseButton.onClick.AddListener(HideUpgradePanel);
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(QuitGame);
            exitButton.onClick.AddListener(QuitGame);
        }
    }

    private void PopulateStageButtons()
    {
        if (contentRoot == null || stageButtonTemplate == null)
        {
            Debug.LogWarning("Stage selection UI could not be populated because Content or BtnStage was not found.", this);
            return;
        }

        IReadOnlyList<StageData> stages = StageTable.Stages;
        ClearGeneratedStageButtons();

        Vector2 templatePosition = GetTemplateAnchoredPosition();
        float stageButtonHeight = GetStageButtonHeight();

        for (int i = 0; i < stages.Count; i++)
        {
            StageData stage = stages[i];
            GameObject stageButton = i == 0
                ? stageButtonTemplate
                : Instantiate(stageButtonTemplate, contentRoot);

            stageButton.name = $"{StageButtonName}_{stage.StageId}";
            stageButton.SetActive(true);
            SetStageButtonPosition(stageButton, templatePosition, stageButtonHeight, i);
            ApplyStageButton(stageButton, stage);
            stageButtons.Add(stageButton);
        }

        ResizeContent(stages.Count, stageButtonHeight);

        if (stages.Count == 0)
        {
            stageButtonTemplate.SetActive(false);
        }
    }

    private Vector2 GetTemplateAnchoredPosition()
    {
        RectTransform templateTransform = stageButtonTemplate.GetComponent<RectTransform>();
        return templateTransform != null ? templateTransform.anchoredPosition : Vector2.zero;
    }

    private float GetStageButtonHeight()
    {
        RectTransform templateTransform = stageButtonTemplate.GetComponent<RectTransform>();
        return templateTransform != null ? Mathf.Max(1f, templateTransform.rect.height) : 1f;
    }

    private static void SetStageButtonPosition(GameObject stageButton, Vector2 templatePosition, float stageButtonHeight, int index)
    {
        RectTransform rectTransform = stageButton.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(
                templatePosition.x,
                templatePosition.y - (index * (stageButtonHeight + StageButtonSpacing)));
        }
    }

    private void ResizeContent(int stageCount, float stageButtonHeight)
    {
        RectTransform contentTransform = contentRoot as RectTransform;
        if (contentTransform != null)
        {
            float contentHeight = Mathf.Max(
                contentTransform.sizeDelta.y,
                (stageCount * stageButtonHeight) + (Mathf.Max(0, stageCount - 1) * StageButtonSpacing));
            contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, contentHeight);
        }
    }

    private void ClearGeneratedStageButtons()
    {
        stageButtons.Clear();

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            if (child.gameObject == stageButtonTemplate)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void ApplyStageButton(GameObject stageButtonObject, StageData stage)
    {
        TMP_Text stageNameText = FindChildComponent<TMP_Text>(stageButtonObject.transform, StageTextName);
        if (stageNameText != null)
        {
            stageNameText.text = stage.Name;
        }

        Image stageImage = FindChildComponent<Image>(stageButtonObject.transform, StageImageName)
            ?? FindChildComponent<Image>(stageButtonObject.transform, AlternateStageImageName);
        if (stageImage != null)
        {
            Sprite sprite = LoadStageSprite(stage.Image);
            stageImage.sprite = sprite;
            stageImage.enabled = sprite != null;
            stageImage.preserveAspect = true;
        }

        Button stageButton = stageButtonObject.GetComponent<Button>();
        if (stageButton != null)
        {
            stageButton.onClick.RemoveAllListeners();
            stageButton.onClick.AddListener(() => LoadStage(stage.StageId));
        }
    }

    private void PopulateUpgradeCells()
    {
        if (upgradeContentRoot == null || upgradeCellTemplate == null)
        {
            return;
        }

        ClearGeneratedUpgradeCells();
        IReadOnlyList<PlayerUpgradeStat> stats = PermanentUpgradeManager.Stats;
        Vector2 templatePosition = GetUpgradeCellTemplateAnchoredPosition();
        float cellHeight = GetUpgradeCellHeight();

        for (int i = 0; i < stats.Count; i++)
        {
            PlayerUpgradeStat stat = stats[i];
            GameObject upgradeCell = i == 0
                ? upgradeCellTemplate
                : Instantiate(upgradeCellTemplate, upgradeContentRoot);

            upgradeCell.name = $"{UpgradeCellName}_{stat}";
            upgradeCell.SetActive(true);
            SetUpgradeCellPosition(upgradeCell, templatePosition, cellHeight, i);
            BindUpgradeCell(upgradeCell, stat);
            upgradeCells.Add(upgradeCell);
        }

        ResizeUpgradeContent(stats.Count, cellHeight);
        ResetUpgradeScrollPosition();
        if (stats.Count == 0)
        {
            upgradeCellTemplate.SetActive(false);
        }
    }

    private void RefreshUpgradeCells()
    {
        for (int i = 0; i < upgradeCells.Count; i++)
        {
            GameObject upgradeCell = upgradeCells[i];
            if (upgradeCell == null)
            {
                continue;
            }

            string statName = upgradeCell.name.Replace($"{UpgradeCellName}_", string.Empty);
            if (Enum.TryParse(statName, out PlayerUpgradeStat stat))
            {
                BindUpgradeCell(upgradeCell, stat);
            }
        }

        UpdateTotalCoinText();
    }

    private void ClearGeneratedUpgradeCells()
    {
        upgradeCells.Clear();
        if (upgradeContentRoot == null)
        {
            return;
        }

        for (int i = upgradeContentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = upgradeContentRoot.GetChild(i);
            if (child.gameObject == upgradeCellTemplate)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private void BindUpgradeCell(GameObject upgradeCell, PlayerUpgradeStat stat)
    {
        UpgradeStatusData currentData = PermanentUpgradeManager.GetCurrentData(stat);
        UpgradeStatusData nextData = PermanentUpgradeManager.GetNextData(stat);
        int currentLevel = PermanentUpgradeManager.GetLevel(stat);

        TMP_Text statusText = FindChildComponent<TMP_Text>(upgradeCell.transform, StatusTextName);
        if (statusText != null)
        {
            statusText.text = currentData != null ? currentData.StatName : stat.ToString();
        }

        Button buyButton = FindChildComponent<Button>(upgradeCell.transform, BuyButtonName);
        TMP_Text buyText = buyButton != null ? buyButton.GetComponentInChildren<TMP_Text>(true) : null;
        if (buyText != null)
        {
            buyText.text = nextData != null ? nextData.CoinValue.ToString() : "MAX";
        }

        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.interactable = nextData != null && CoinManager.LoadTotalCoins() >= nextData.CoinValue;
            buyButton.onClick.AddListener(() => TryBuyUpgrade(stat));
        }

        ApplyDotImages(upgradeCell.transform, currentLevel);
    }

    private void TryBuyUpgrade(PlayerUpgradeStat stat)
    {
        if (!PermanentUpgradeManager.TryUpgrade(stat, out _))
        {
            RefreshUpgradeCells();
            return;
        }

        RefreshUpgradeCells();
    }

    private void ApplyDotImages(Transform upgradeCellTransform, int currentLevel)
    {
        Transform dotsRoot = FindChild(upgradeCellTransform, DotsName);
        if (dotsRoot == null)
        {
            return;
        }

        for (int i = 1; i <= PermanentUpgradeManager.MaxUpgradeLevel; i++)
        {
            Transform dot = FindChild(dotsRoot, $"{DotNamePrefix}{i}");
            Image image = dot != null ? dot.GetComponent<Image>() : null;
            if (image == null)
            {
                continue;
            }

            if (dotInactiveSprite == null)
            {
                dotInactiveSprite = image.sprite;
            }

            image.sprite = i <= currentLevel && dotActiveSprite != null ? dotActiveSprite : dotInactiveSprite;
        }
    }

    private void ResetUpgradeScrollPosition()
    {
        if (upgradeScrollRect == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        upgradeScrollRect.verticalNormalizedPosition = 1f;
    }

    private Vector2 GetUpgradeCellTemplateAnchoredPosition()
    {
        RectTransform templateTransform = upgradeCellTemplate.GetComponent<RectTransform>();
        return templateTransform != null ? templateTransform.anchoredPosition : Vector2.zero;
    }

    private float GetUpgradeCellHeight()
    {
        GridLayoutGroup gridLayoutGroup = upgradeContentRoot != null
            ? upgradeContentRoot.GetComponent<GridLayoutGroup>()
            : null;
        if (gridLayoutGroup != null && gridLayoutGroup.cellSize.y > 0f)
        {
            return gridLayoutGroup.cellSize.y;
        }

        RectTransform templateTransform = upgradeCellTemplate.GetComponent<RectTransform>();
        if (templateTransform != null && templateTransform.rect.height > 0f)
        {
            return templateTransform.rect.height;
        }

        LayoutElement layoutElement = upgradeCellTemplate.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.preferredHeight > 0f)
        {
            return layoutElement.preferredHeight;
        }

        return 20f;
    }

    private float GetUpgradeCellSpacing()
    {
        GridLayoutGroup gridLayoutGroup = upgradeContentRoot != null
            ? upgradeContentRoot.GetComponent<GridLayoutGroup>()
            : null;
        return gridLayoutGroup != null ? gridLayoutGroup.spacing.y : StageButtonSpacing;
    }

    private void SetUpgradeCellPosition(GameObject upgradeCell, Vector2 templatePosition, float cellHeight, int index)
    {
        if (upgradeContentRoot != null && upgradeContentRoot.GetComponent<GridLayoutGroup>() != null)
        {
            return;
        }

        RectTransform rectTransform = upgradeCell.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(
                templatePosition.x,
                templatePosition.y - (index * (cellHeight + GetUpgradeCellSpacing())));
        }
    }

    private void ResizeUpgradeContent(int cellCount, float cellHeight)
    {
        RectTransform contentTransform = upgradeContentRoot as RectTransform;
        if (contentTransform == null)
        {
            return;
        }

        float contentHeight = CalculateUpgradeContentHeight(cellCount, cellHeight);
        contentTransform.sizeDelta = new Vector2(contentTransform.sizeDelta.x, contentHeight);
    }

    private float CalculateUpgradeContentHeight(int cellCount, float cellHeight)
    {
        GridLayoutGroup gridLayoutGroup = upgradeContentRoot != null
            ? upgradeContentRoot.GetComponent<GridLayoutGroup>()
            : null;
        if (gridLayoutGroup != null)
        {
            int rowCount = gridLayoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount
                ? Mathf.CeilToInt(cellCount / (float)Mathf.Max(1, gridLayoutGroup.constraintCount))
                : cellCount;

            return gridLayoutGroup.padding.top
                + gridLayoutGroup.padding.bottom
                + (rowCount * gridLayoutGroup.cellSize.y)
                + (Mathf.Max(0, rowCount - 1) * gridLayoutGroup.spacing.y);
        }

        return (cellCount * cellHeight) + (Mathf.Max(0, cellCount - 1) * GetUpgradeCellSpacing());
    }

    private void LoadStage(int stageId)
    {
        StageSelection.SelectStage(stageId);
        SceneManager.LoadScene(GameSceneName);
    }

    private GameObject FindUpgradeCellTemplate()
    {
        if (upgradeContentRoot != null)
        {
            Transform directChild = upgradeContentRoot.Find(UpgradeCellName);
            if (directChild != null)
            {
                return directChild.gameObject;
            }

            for (int i = 0; i < upgradeContentRoot.childCount; i++)
            {
                Transform child = upgradeContentRoot.GetChild(i);
                if (child.name.StartsWith(UpgradeCellName, StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }
        }

        Transform panelTransform = upgradePanel != null ? upgradePanel.transform : null;
        Transform panelUpgradeCell = FindChild(panelTransform, UpgradeCellName);
        if (panelUpgradeCell != null)
        {
            return panelUpgradeCell.gameObject;
        }

        return Resources.Load<GameObject>(UpgradeCellName);
    }

    private void CacheDotSprites()
    {
        UpgradeDotSpriteCatalog catalog = Resources.Load<UpgradeDotSpriteCatalog>(DotSpriteCatalogResourcePath);
        if (catalog != null)
        {
            dotInactiveSprite = catalog.InactiveSprite;
            dotActiveSprite = catalog.ActiveSprite;
        }
    }

    private GameObject FindStageButtonTemplate()
    {
        GameObject template = FindStageButtonTemplateIn(contentRoot);
        if (template != null)
        {
            return template;
        }

        Transform panelTransform = gameStartPanel != null ? gameStartPanel.transform : null;
        Transform panelStageButton = FindChild(panelTransform, StageButtonName);
        return panelStageButton != null ? panelStageButton.gameObject : FindSceneGameObject(StageButtonName);
    }

    private static GameObject FindStageButtonTemplateIn(Transform root)
    {
        if (root == null)
        {
            return null;
        }

        Transform directChild = root.Find(StageButtonName);
        if (directChild != null)
        {
            return directChild.gameObject;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name.StartsWith(StageButtonName, StringComparison.Ordinal))
            {
                return child.gameObject;
            }
        }

        return null;
    }

    private static Sprite LoadStageSprite(string imageName)
    {
        if (string.IsNullOrWhiteSpace(imageName))
        {
            return null;
        }

        string trimmedName = imageName.Trim();
        Sprite sprite = Resources.Load<Sprite>(trimmedName) ?? Resources.Load<Sprite>($"Sprites/{trimmedName}");
        if (sprite != null)
        {
            return sprite;
        }

        StageSpriteCatalog catalog = Resources.Load<StageSpriteCatalog>(StageSpriteCatalogPath);
        return catalog != null && catalog.TryGetSprite(trimmedName, out sprite) ? sprite : null;
    }

    private Button FindButtonInUpgradePanel(string objectName)
    {
        GameObject gameObject = FindUpgradePanelChild(objectName);
        return gameObject != null ? gameObject.GetComponent<Button>() : null;
    }

    private ScrollRect FindUpgradeScrollRect()
    {
        Transform panelTransform = upgradePanel != null ? upgradePanel.transform : null;
        return FindChildComponent<ScrollRect>(panelTransform, "Scroll View")
            ?? (upgradeContentRoot != null ? upgradeContentRoot.GetComponentInParent<ScrollRect>(true) : null);
    }

    private void ConfigureUpgradeScrollRect()
    {
        if (upgradeScrollRect == null)
        {
            return;
        }

        RectTransform contentTransform = upgradeContentRoot as RectTransform;
        if (contentTransform != null)
        {
            upgradeScrollRect.content = contentTransform;
        }

        upgradeScrollRect.horizontal = false;
        upgradeScrollRect.vertical = true;

        if (upgradeScrollRect.viewport == null && contentTransform != null)
        {
            upgradeScrollRect.viewport = contentTransform.parent as RectTransform;
        }

        if (upgradeScrollRect.verticalScrollbar == null)
        {
            upgradeScrollRect.verticalScrollbar = FindChildComponent<Scrollbar>(upgradeScrollRect.transform, "Scrollbar Vertical");
        }
    }

    private GameObject FindUpgradePanelChild(string objectName)
    {
        Transform panelTransform = upgradePanel != null ? upgradePanel.transform : null;
        Transform child = FindChild(panelTransform, objectName);
        return child != null ? child.gameObject : null;
    }

    private Button FindButtonInGameStartPanel(string objectName)
    {
        GameObject gameObject = FindGameStartPanelChild(objectName);
        return gameObject != null ? gameObject.GetComponent<Button>() : null;
    }

    private GameObject FindGameStartPanelChild(string objectName)
    {
        Transform panelTransform = gameStartPanel != null ? gameStartPanel.transform : null;
        Transform child = FindChild(panelTransform, objectName);
        return child != null ? child.gameObject : null;
    }

    private static Button FindButton(string objectName)
    {
        GameObject gameObject = FindSceneGameObject(objectName);
        return gameObject != null ? gameObject.GetComponent<Button>() : null;
    }

    private static GameObject FindSceneGameObject(string objectName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject candidate = allObjects[i];
            if (candidate.name == objectName && candidate.scene.IsValid() && candidate.hideFlags == HideFlags.None)
            {
                return candidate;
            }
        }

        return null;
    }

    private static T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform child = FindChild(root, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }

            Transform nestedChild = FindChild(child, childName);
            if (nestedChild != null)
            {
                return nestedChild;
            }
        }

        return null;
    }
}
