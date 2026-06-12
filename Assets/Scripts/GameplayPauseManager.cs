using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameplayPauseManager
{
    private static int pauseRequestCount;
    private static float timeScaleBeforePause = 1f;

    public static bool IsPaused => pauseRequestCount > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeOnLoad()
    {
        ResetPauseState(1f);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadedCallback()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ResetPauseState(1f);
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            ResetPauseState(1f);
        }
    }

    public static void RequestPause()
    {
        if (pauseRequestCount == 0)
        {
            timeScaleBeforePause = Time.timeScale;
            Time.timeScale = 0f;
        }

        pauseRequestCount++;
    }

    public static void ReleasePause()
    {
        if (pauseRequestCount <= 0)
        {
            pauseRequestCount = 0;
            return;
        }

        pauseRequestCount--;
        if (pauseRequestCount == 0)
        {
            Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        }
    }

    public static void ResetPause()
    {
        ResetPauseState(timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f);
    }

    private static void ResetPauseState(float timeScale)
    {
        pauseRequestCount = 0;
        timeScaleBeforePause = 1f;
        Time.timeScale = timeScale > 0f ? timeScale : 1f;
    }
}
