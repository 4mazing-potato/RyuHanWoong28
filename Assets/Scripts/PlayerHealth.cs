using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStatus))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Invincibility")]
    [SerializeField] private float invincibleTime = 1f;
    [SerializeField] private float blinkInterval = 0.1f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("UI")]
    [SerializeField] private Image hpBar;

    [Header("Events")]
    [SerializeField] private UnityEvent onDied;

    private Coroutine invincibleCoroutine;
    private Color originalColor;
    private bool isInvincible;
    private bool isDead;
    private PlayerStatus playerStatus;

    public float MaxHp => playerStatus != null ? playerStatus.CurrentMaxHP : 0f;
    public float CurrentHp => playerStatus != null ? playerStatus.CurrentHP : 0f;
    public bool IsInvincible => isInvincible;
    public bool IsDead => isDead;
    public UnityEvent OnDied => onDied;

    private void Awake()
    {
        CacheReferences();
        if (playerStatus != null)
        {
            playerStatus.HealthChanged += UpdateHpBar;
            playerStatus.InitializeHealthForBattle();
        }
        EnsureCollisionComponents();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        UpdateHpBar();
    }

    private void OnDestroy()
    {
        if (playerStatus != null)
        {
            playerStatus.HealthChanged -= UpdateHpBar;
        }
    }

    public bool TakeDamage(float damage)
    {
        if (GameplayPauseManager.IsPaused || isDead || isInvincible || damage <= 0f)
        {
            return false;
        }

        playerStatus.TakeDamage(damage);
        UpdateHpBar();

        if (playerStatus.CurrentHP <= 0f)
        {
            Die();
            return true;
        }

        StartInvincibility();
        return true;
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
        {
            return;
        }

        playerStatus.Heal(amount);
        UpdateHpBar();
    }

    public void IncreaseMaxHp(float amount, bool healByIncreaseAmount = true)
    {
        if (amount <= 0f)
        {
            return;
        }

        playerStatus.IncreaseMaxHP(amount, healByIncreaseAmount && !isDead);
        UpdateHpBar();
    }

    public void SetMaxHp(float newMaxHp, bool fillHp = false)
    {
        playerStatus.SetCurrentMaxHP(newMaxHp, fillHp);
        UpdateHpBar();
    }

    public void RefreshHealthView()
    {
        UpdateHpBar();
    }

    private void CacheReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (hpBar == null)
        {
            Transform hpBarTransform = transform.Find("Hp_Bar");
            if (hpBarTransform != null)
            {
                hpBar = hpBarTransform.GetComponent<Image>();
            }
        }
    }

    private void EnsureCollisionComponents()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (GetComponent<Collider2D>() == null)
        {
            CapsuleCollider2D capsuleCollider = gameObject.AddComponent<CapsuleCollider2D>();
            capsuleCollider.isTrigger = false;
        }
    }

    private void UpdateHpBar()
    {
        if (hpBar != null)
        {
            float maxHp = playerStatus != null ? playerStatus.CurrentMaxHP : 0f;
            float currentHp = playerStatus != null ? playerStatus.CurrentHP : 0f;
            hpBar.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        }
    }

    private void StartInvincibility()
    {
        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
        }

        invincibleCoroutine = StartCoroutine(InvincibilityRoutine());
    }

    private IEnumerator InvincibilityRoutine()
    {
        isInvincible = true;

        float elapsed = 0f;
        float safeBlinkInterval = Mathf.Max(0.01f, blinkInterval);
        while (elapsed < invincibleTime)
        {
            SetSpriteAlpha(0.35f);
            yield return new WaitForSeconds(safeBlinkInterval);
            elapsed += safeBlinkInterval;

            SetSpriteAlpha(1f);
            yield return new WaitForSeconds(safeBlinkInterval);
            elapsed += safeBlinkInterval;
        }

        SetSpriteAlpha(1f);
        isInvincible = false;
        invincibleCoroutine = null;
    }

    private void SetSpriteAlpha(float alpha)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Color color = originalColor;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;

        if (invincibleCoroutine != null)
        {
            StopCoroutine(invincibleCoroutine);
            invincibleCoroutine = null;
        }

        isInvincible = false;
        SetSpriteAlpha(1f);
        onDied?.Invoke();
    }
}
