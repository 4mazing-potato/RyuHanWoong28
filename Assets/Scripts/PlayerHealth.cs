using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHp = 10f;
    [SerializeField] private float currentHp;

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

    public float MaxHp => maxHp;
    public float CurrentHp => currentHp;
    public bool IsInvincible => isInvincible;
    public bool IsDead => isDead;
    public UnityEvent OnDied => onDied;

    private void Awake()
    {
        maxHp = Mathf.Max(1f, maxHp);

        if (currentHp <= 0f || currentHp > maxHp)
        {
            currentHp = maxHp;
        }

        CacheReferences();
        EnsureCollisionComponents();
        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        UpdateHpBar();
    }

    public bool TakeDamage(float damage)
    {
        if (isDead || isInvincible || damage <= 0f)
        {
            return false;
        }

        currentHp = Mathf.Max(0f, currentHp - damage);
        UpdateHpBar();

        if (currentHp <= 0f)
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

        currentHp = Mathf.Min(maxHp, currentHp + amount);
        UpdateHpBar();
    }

    public void IncreaseMaxHp(float amount, bool healByIncreaseAmount = true)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxHp += amount;

        if (healByIncreaseAmount && !isDead)
        {
            currentHp = Mathf.Min(maxHp, currentHp + amount);
        }
        else
        {
            currentHp = Mathf.Min(currentHp, maxHp);
        }

        UpdateHpBar();
    }

    public void SetMaxHp(float newMaxHp, bool fillHp = false)
    {
        maxHp = Mathf.Max(1f, newMaxHp);
        currentHp = fillHp ? maxHp : Mathf.Min(currentHp, maxHp);
        UpdateHpBar();
    }

    private void CacheReferences()
    {
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
