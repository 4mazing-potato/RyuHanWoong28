using TMPro;
using UnityEngine;

public class PlayerExperience : MonoBehaviour
{
    private const int StartingLevel = 1;
    private const string LevelTextObjectName = "TextLevel";
    private const string ExpBarFillObjectName = "ExpBar_Fill";

    [Header("Experience")]
    [SerializeField] private int currentLevel = StartingLevel;
    [SerializeField] private int currentExp;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private ExpBarController expBarController;

    public int CurrentLevel => currentLevel;
    public int CurrentExp => currentExp;
    public int NeedExp => LevelXpTable.GetNeedXp(currentLevel);

    private void Awake()
    {
        currentLevel = Mathf.Max(StartingLevel, currentLevel);
        currentExp = Mathf.Max(0, currentExp);
        CacheUiReferences();
        NormalizeExperienceWithoutAnimation();
        UpdateLevelText();
        UpdateExpBarInstant();
    }

    public void AddExp(int expAmount)
    {
        if (expAmount <= 0)
        {
            return;
        }

        int startLevel = currentLevel;
        int startExp = currentExp;

        currentExp += expAmount;
        ProcessLevelUps();
        int gainedLevelCount = currentLevel - startLevel;
        UpdateLevelText();

        if (gainedLevelCount > 0)
        {
            LevelUpSkillUpgradeController.EnsureInstance().EnqueueLevelUps(gainedLevelCount);
        }

        if (expBarController != null)
        {
            expBarController.PlayExperienceGain(startLevel, startExp, currentLevel, currentExp);
        }
    }

    private void ProcessLevelUps()
    {
        while (currentExp >= NeedExp)
        {
            currentExp -= NeedExp;
            currentLevel++;
        }
    }

    private void NormalizeExperienceWithoutAnimation()
    {
        ProcessLevelUps();
    }

    private void CacheUiReferences()
    {
        if (levelText == null)
        {
            GameObject levelTextObject = GameObject.Find(LevelTextObjectName);
            if (levelTextObject != null)
            {
                levelText = levelTextObject.GetComponent<TextMeshProUGUI>();
            }
        }

        if (expBarController == null)
        {
            GameObject expBarObject = GameObject.Find(ExpBarFillObjectName);
            if (expBarObject != null)
            {
                expBarController = expBarObject.GetComponent<ExpBarController>();
            }
        }
    }

    private void UpdateLevelText()
    {
        if (levelText != null)
        {
            levelText.text = $"Lv.{currentLevel}";
        }
    }

    private void UpdateExpBarInstant()
    {
        if (expBarController != null)
        {
            expBarController.SetInstantRatio(GetCurrentXpRatio());
        }
    }

    private float GetCurrentXpRatio()
    {
        int needExp = NeedExp;
        return needExp > 0 ? Mathf.Clamp01((float)currentExp / needExp) : 0f;
    }
}
