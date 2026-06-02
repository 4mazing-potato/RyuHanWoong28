using UnityEngine;

public class ExplosionEffectController : MonoBehaviour
{
    [Header("Effect")]
    [Tooltip("이펙트가 최종적으로 퍼져나갈 원형 크기입니다.")]
    [SerializeField] private float effectsSize = 5f;
    [Tooltip("이펙트가 자연스럽게 소멸되기까지 걸리는 시간입니다.")]
    [SerializeField] private float effectLifeTime = 0.35f;
    [Tooltip("이펙트가 시작할 때의 크기 비율입니다.")]
    [SerializeField] private float startSizeRatio = 0.15f;

    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private float elapsedTime;

    public float EffectsSize
    {
        get => effectsSize;
        set => effectsSize = Mathf.Max(0f, value);
    }

    public float EffectLifeTime
    {
        get => effectLifeTime;
        set => effectLifeTime = Mathf.Max(0.01f, value);
    }

    private void Awake()
    {
        CacheSpriteRenderer();
    }

    private void OnEnable()
    {
        ResetEffect();
    }

    private void OnValidate()
    {
        effectsSize = Mathf.Max(0f, effectsSize);
        effectLifeTime = Mathf.Max(0.01f, effectLifeTime);
        startSizeRatio = Mathf.Clamp01(startSizeRatio);
    }

    private void Update()
    {
        if (GameplayPauseManager.IsPaused)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsedTime / effectLifeTime);
        float easedProgress = 1f - (1f - progress) * (1f - progress);

        float currentSize = Mathf.Lerp(effectsSize * startSizeRatio, effectsSize, easedProgress);
        ApplyWorldSize(currentSize);

        if (spriteRenderer != null)
        {
            Color color = originalColor;
            color.a = Mathf.Lerp(originalColor.a, 0f, progress);
            spriteRenderer.color = color;
        }

        if (progress >= 1f)
        {
            gameObject.SetActive(false);
        }
    }

    public void Play(float size)
    {
        EffectsSize = size;
        gameObject.SetActive(true);
        ResetEffect();
    }

    private void ResetEffect()
    {
        elapsedTime = 0f;
        CacheSpriteRenderer();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            originalColor.a = Mathf.Max(originalColor.a, 1f);
            spriteRenderer.color = originalColor;
        }

        ApplyWorldSize(effectsSize * startSizeRatio);
    }

    private void ApplyWorldSize(float worldSize)
    {
        float spriteSize = 1f;
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Vector2 spriteBoundsSize = spriteRenderer.sprite.bounds.size;
            spriteSize = Mathf.Max(Mathf.Max(spriteBoundsSize.x, spriteBoundsSize.y), 0.0001f);
        }

        transform.localScale = Vector3.one * (worldSize / spriteSize);
    }

    private void CacheSpriteRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }
}
