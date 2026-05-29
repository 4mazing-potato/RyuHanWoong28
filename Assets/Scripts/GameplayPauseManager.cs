using UnityEngine;

public static class GameplayPauseManager
{
    private static int pauseRequestCount;
    private static float timeScaleBeforePause = 1f;

    public static bool IsPaused => pauseRequestCount > 0;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void InitializeOnLoad()
    {
        pauseRequestCount = 0;
        timeScaleBeforePause = 1f;
        Time.timeScale = 1f;
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
        pauseRequestCount = 0;
        Time.timeScale = timeScaleBeforePause > 0f ? timeScaleBeforePause : 1f;
        timeScaleBeforePause = 1f;
    }
}
