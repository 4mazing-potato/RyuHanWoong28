using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float baseAttack = 1f;
    [SerializeField] private float currentAttack = 1f;

    [Header("Health")]
    [SerializeField] private float baseMaxHP = 10f;
    [SerializeField] private float currentMaxHP = 10f;
    [SerializeField] private float currentHP = 10f;

    private float attackUpMultiplier = 1f;
    private float hpUpMultiplier = 1f;
    private bool healthInitialized;

    public float BaseAttack => baseAttack;
    public float CurrentAttack => currentAttack;
    public float BaseMaxHP => baseMaxHP;
    public float CurrentMaxHP => currentMaxHP;
    public float CurrentHP => currentHP;

    private void Awake()
    {
        RecalculateCurrentAttack();
        InitializeHealthForBattle();
    }

    private void OnValidate()
    {
        baseAttack = Mathf.Max(0f, baseAttack);
        baseMaxHP = Mathf.Max(1f, baseMaxHP);
        currentMaxHP = Mathf.Max(1f, currentMaxHP);
        currentHP = Mathf.Clamp(currentHP, 0f, currentMaxHP);
        RecalculateCurrentAttack();
    }

    public void ApplyAttackUpPercent(float percentValue)
    {
        attackUpMultiplier = Mathf.Max(0f, percentValue) * 0.01f;
        RecalculateCurrentAttack();
    }

    public void InitializeHealthForBattle()
    {
        hpUpMultiplier = 1f;
        currentMaxHP = Mathf.Max(1f, baseMaxHP);
        currentHP = currentMaxHP;
        healthInitialized = true;
    }

    public void ApplyHPUpPercent(float percentValue)
    {
        EnsureHealthInitialized();

        hpUpMultiplier = Mathf.Max(0f, percentValue) * 0.01f;
        currentMaxHP = Mathf.Max(1f, baseMaxHP * hpUpMultiplier);
        currentHP = Mathf.Min(currentMaxHP, currentHP * hpUpMultiplier);
    }

    public bool TakeDamage(float damage)
    {
        EnsureHealthInitialized();

        if (damage <= 0f)
        {
            return false;
        }

        currentHP = Mathf.Max(0f, currentHP - damage);
        return true;
    }

    public void Heal(float amount)
    {
        EnsureHealthInitialized();

        if (amount <= 0f)
        {
            return;
        }

        currentHP = Mathf.Min(currentMaxHP, currentHP + amount);
    }

    public void IncreaseMaxHP(float amount, bool healByIncreaseAmount = true)
    {
        EnsureHealthInitialized();

        if (amount <= 0f)
        {
            return;
        }

        currentMaxHP += amount;
        currentHP = healByIncreaseAmount ? Mathf.Min(currentMaxHP, currentHP + amount) : Mathf.Min(currentHP, currentMaxHP);
    }

    public void SetCurrentMaxHP(float newMaxHP, bool fillHp = false)
    {
        EnsureHealthInitialized();

        currentMaxHP = Mathf.Max(1f, newMaxHP);
        currentHP = fillHp ? currentMaxHP : Mathf.Min(currentHP, currentMaxHP);
    }

    public float CalculateDamage(float damageMultiplier)
    {
        return currentAttack * Mathf.Max(0f, damageMultiplier);
    }

    private void EnsureHealthInitialized()
    {
        if (!healthInitialized)
        {
            InitializeHealthForBattle();
        }
    }

    private void RecalculateCurrentAttack()
    {
        currentAttack = Mathf.Max(0f, baseAttack) * attackUpMultiplier;
    }
}
