using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ExpBarController : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float fillDuration = 0.2f;
    [SerializeField] private float fullHoldDuration = 0.12f;

    private Image fillImage;
    private Coroutine fillCoroutine;

    private void Awake()
    {
        fillImage = GetComponent<Image>();
        SetInstantRatio(fillImage != null ? fillImage.fillAmount : 0f);
    }

    public void SetInstantRatio(float xpRatio)
    {
        EnsureFillImage();
        if (fillImage != null)
        {
            fillImage.fillAmount = Mathf.Clamp01(xpRatio);
        }
    }

    public void PlayExperienceGain(int startLevel, int startExp, int endLevel, int endExp)
    {
        EnsureFillImage();
        if (fillImage == null)
        {
            return;
        }

        if (fillCoroutine != null)
        {
            StopCoroutine(fillCoroutine);
        }

        fillCoroutine = StartCoroutine(PlayExperienceGainRoutine(startLevel, startExp, endLevel, endExp));
    }

    private IEnumerator PlayExperienceGainRoutine(int startLevel, int startExp, int endLevel, int endExp)
    {
        int level = Mathf.Max(1, startLevel);
        int currentExp = Mathf.Max(0, startExp);
        SetInstantRatio(GetRatio(level, currentExp));

        while (level < endLevel)
        {
            yield return AnimateFillTo(1f);
            yield return new WaitForSeconds(Mathf.Max(0f, fullHoldDuration));
            SetInstantRatio(0f);
            level++;
            currentExp = 0;
        }

        float finalRatio = GetRatio(endLevel, endExp);
        if (level > startLevel)
        {
            SetInstantRatio(finalRatio);
        }
        else
        {
            yield return AnimateFillTo(finalRatio);
        }

        fillCoroutine = null;
    }

    private IEnumerator AnimateFillTo(float targetRatio)
    {
        float startRatio = fillImage.fillAmount;
        float clampedTarget = Mathf.Clamp01(targetRatio);
        float duration = Mathf.Max(0.01f, fillDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            fillImage.fillAmount = Mathf.Lerp(startRatio, clampedTarget, SmoothStep(t));
            yield return null;
        }

        fillImage.fillAmount = clampedTarget;
    }

    private float GetRatio(int level, int exp)
    {
        int needExp = LevelXpTable.GetNeedXp(level);
        return needExp > 0 ? Mathf.Clamp01((float)Mathf.Max(0, exp) / needExp) : 0f;
    }

    private void EnsureFillImage()
    {
        if (fillImage == null)
        {
            fillImage = GetComponent<Image>();
        }
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }
}
