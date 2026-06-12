using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageResultController : MonoBehaviour
{
    private const string GameSceneName = "GameScene";
    private const string HomeSceneName = "HomeScene";
    private const string ResultHudName = "HUD Result";
    private const string SuccessImageName = "Success";
    private const string FailImageName = "Fail";
    private const string HomeButtonName = "Btn_Home";

    [SerializeField] private GameObject resultHud;
    [SerializeField] private GameObject successImage;
    [SerializeField] private GameObject failImage;
    [SerializeField] private Button homeButton;
    [SerializeField] private MonsterSpawnManager monsterSpawnManager;

    private bool isShowingResult;
    private bool hasSavedSessionCoin;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapForGameScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != GameSceneName || FindObjectOfType<StageResultController>() != null)
        {
            return;
        }

        new GameObject(nameof(StageResultController)).AddComponent<StageResultController>();
    }

    private void Awake()
    {
        GameplayPauseManager.ResetPause();
        CacheReferences();
        ConfigureHomeButton();
        HideResult();
        RegisterStageEvents();
    }

    private void Start()
    {
        CacheReferences();
        ConfigureHomeButton();
        HideResult();
        RegisterStageEvents();
        ShowAlreadyCompletedStageResult();
    }

    private void OnDestroy()
    {
        UnregisterStageEvents();
        if (homeButton != null)
        {
            homeButton.onClick.RemoveListener(LoadHomeScene);
        }
    }

    public void ShowSuccess()
    {
        ShowResult(true);
    }

    public void ShowFail()
    {
        ShowResult(false);
    }

    public void LoadHomeScene()
    {
        GameplayPauseManager.ResetPause();
        SceneManager.LoadScene(HomeSceneName);
    }

    private void ShowAlreadyCompletedStageResult()
    {
        if (monsterSpawnManager == null || !monsterSpawnManager.StageCompleted || isShowingResult)
        {
            return;
        }

        ShowResult(monsterSpawnManager.StageSucceeded);
    }

    private void ShowResult(bool succeeded)
    {
        CacheReferences();
        ConfigureHomeButton();
        SaveSessionCoinOnce();

        if (resultHud != null)
        {
            resultHud.SetActive(true);
        }

        if (successImage != null)
        {
            successImage.SetActive(succeeded);
        }

        if (failImage != null)
        {
            failImage.SetActive(!succeeded);
        }

        if (homeButton != null)
        {
            homeButton.gameObject.SetActive(true);
            homeButton.interactable = true;
        }

        isShowingResult = true;
    }

    private void SaveSessionCoinOnce()
    {
        if (hasSavedSessionCoin)
        {
            return;
        }

        CoinManager coinManager = CoinManager.Instance ?? FindObjectOfType<CoinManager>();
        if (coinManager == null)
        {
            return;
        }

        hasSavedSessionCoin = true;
        CoinManager.AddToTotalCoins(coinManager.CurrentCoin);
    }

    private void HideResult()
    {
        if (isShowingResult)
        {
            return;
        }

        if (resultHud != null)
        {
            resultHud.SetActive(false);
        }

        if (successImage != null)
        {
            successImage.SetActive(false);
        }

        if (failImage != null)
        {
            failImage.SetActive(false);
        }
    }

    private void RegisterStageEvents()
    {
        if (monsterSpawnManager == null)
        {
            monsterSpawnManager = FindObjectOfType<MonsterSpawnManager>();
        }

        if (monsterSpawnManager == null)
        {
            return;
        }

        monsterSpawnManager.OnStageSucceeded.RemoveListener(ShowSuccess);
        monsterSpawnManager.OnStageFailed.RemoveListener(ShowFail);
        monsterSpawnManager.OnStageSucceeded.AddListener(ShowSuccess);
        monsterSpawnManager.OnStageFailed.AddListener(ShowFail);
    }

    private void UnregisterStageEvents()
    {
        if (monsterSpawnManager == null)
        {
            return;
        }

        monsterSpawnManager.OnStageSucceeded.RemoveListener(ShowSuccess);
        monsterSpawnManager.OnStageFailed.RemoveListener(ShowFail);
    }

    private void ConfigureHomeButton()
    {
        if (homeButton == null && resultHud != null)
        {
            GameObject homeButtonObject = FindChildByName(resultHud, HomeButtonName);
            if (homeButtonObject != null)
            {
                homeButton = homeButtonObject.GetComponent<Button>();
            }
        }

        if (homeButton == null)
        {
            return;
        }

        homeButton.onClick.RemoveListener(LoadHomeScene);
        homeButton.onClick.AddListener(LoadHomeScene);
    }

    private void CacheReferences()
    {
        if (resultHud == null)
        {
            resultHud = FindSceneObjectByName(ResultHudName);
        }

        if (resultHud != null)
        {
            if (successImage == null)
            {
                successImage = FindChildByName(resultHud, SuccessImageName);
            }

            if (failImage == null)
            {
                failImage = FindChildByName(resultHud, FailImageName);
            }
        }

        if (monsterSpawnManager == null)
        {
            monsterSpawnManager = FindObjectOfType<MonsterSpawnManager>();
        }
    }

    private static GameObject FindChildByName(GameObject parent, string objectName)
    {
        if (parent == null)
        {
            return null;
        }

        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == objectName)
            {
                return children[i].gameObject;
            }
        }

        return null;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate.name == objectName && candidate.scene.IsValid())
            {
                return candidate;
            }
        }

        return null;
    }
}
