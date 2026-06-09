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
    private const string GameStartPanelName = "Panel_GameStart";
    private const string CloseButtonName = "BtnX";
    private const string ContentName = "Content";
    private const string StageButtonName = "BtnStage";
    private const string StageImageName = "StageImage";
    private const string AlternateStageImageName = "Image";
    private const string StageTextName = "Text (TMP)";
    private const string StageSpriteCatalogPath = "StageSpriteCatalog";
    private const float StageButtonSpacing = 12f;

    private Button startButton;
    private GameObject gameStartPanel;
    private Button closeButton;
    private Transform contentRoot;
    private GameObject stageButtonTemplate;
    private readonly List<GameObject> stageButtons = new List<GameObject>();

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
        HideGameStartPanel();
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
    }

    public void ShowGameStartPanel()
    {
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(true);
        }
    }

    public void HideGameStartPanel()
    {
        if (gameStartPanel != null)
        {
            gameStartPanel.SetActive(false);
        }
    }

    private void CacheReferences()
    {
        startButton = FindButton(StartButtonName);
        if (startButton == null)
        {
            startButton = FindButton(AlternateStartButtonName);
        }

        gameStartPanel = FindSceneGameObject(GameStartPanelName);
        closeButton = FindButton(CloseButtonName);
        GameObject contentObject = FindSceneGameObject(ContentName);
        contentRoot = contentObject != null ? contentObject.transform : null;
        stageButtonTemplate = FindStageButtonTemplate();
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

    private void LoadStage(int stageId)
    {
        StageSelection.SelectStage(stageId);
        SceneManager.LoadScene(GameSceneName);
    }

    private GameObject FindStageButtonTemplate()
    {
        if (contentRoot != null)
        {
            Transform directChild = contentRoot.Find(StageButtonName);
            if (directChild != null)
            {
                return directChild.gameObject;
            }

            for (int i = 0; i < contentRoot.childCount; i++)
            {
                Transform child = contentRoot.GetChild(i);
                if (child.name.StartsWith(StageButtonName, StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }
        }

        return FindSceneGameObject(StageButtonName);
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
