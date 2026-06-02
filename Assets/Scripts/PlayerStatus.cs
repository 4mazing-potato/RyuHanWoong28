using System;
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

    [Header("Pickup")]
    [SerializeField] private float basePickupRadius = 2.5f;
    [SerializeField] private float currentPickupRadius = 2.5f;

    private float attackUpMultiplier = 1f;
    private float hpUpMultiplier = 1f;
    private float pickupRadiusMultiplier = 1f;
    private float healOnDamagePercent;
    private bool healthInitialized;

    public float BaseAttack => baseAttack;
    public float CurrentAttack => currentAttack;
    public float BaseMaxHP => baseMaxHP;
    public float CurrentMaxHP => currentMaxHP;
    public float CurrentHP => currentHP;
    public float BasePickupRadius => basePickupRadius;
    public float CurrentPickupRadius => currentPickupRadius;
    public float HealOnDamagePercent => healOnDamagePercent;

    public event Action HealthChanged;

    private void Awake()
    {
        RecalculateCurrentAttack();
        RecalculateCurrentPickupRadius();
        InitializeHealthForBattle();
    }

    private void OnValidate()
    {
        baseAttack = Mathf.Max(0f, baseAttack);
        baseMaxHP = Mathf.Max(1f, baseMaxHP);
        currentMaxHP = Mathf.Max(1f, currentMaxHP);
        currentHP = Mathf.Clamp(currentHP, 0f, currentMaxHP);
        basePickupRadius = Mathf.Max(0f, basePickupRadius);
        currentPickupRadius = Mathf.Max(0f, currentPickupRadius);
        RecalculateCurrentAttack();
        RecalculateCurrentPickupRadius();
    }

    public void ApplyAttackUpPercent(float percentValue)
    {
        attackUpMultiplier = Mathf.Max(0f, percentValue) * 0.01f;
        RecalculateCurrentAttack();
    }

    public void ApplyPickupRadiusPercent(float percentValue)
    {
        pickupRadiusMultiplier = Mathf.Max(0f, percentValue) * 0.01f;
        RecalculateCurrentPickupRadius();
    }

    public void InitializeHealthForBattle()
    {
        hpUpMultiplier = 1f;
        healOnDamagePercent = 0f;
        currentMaxHP = Mathf.Max(1f, baseMaxHP);
        currentHP = currentMaxHP;
        healthInitialized = true;
        NotifyHealthChanged();
    }

    public void ApplyHPUpPercent(float percentValue)
    {
        EnsureHealthInitialized();

        hpUpMultiplier = Mathf.Max(0f, percentValue) * 0.01f;
        currentMaxHP = Mathf.Max(1f, baseMaxHP * hpUpMultiplier);
        currentHP = Mathf.Min(currentMaxHP, currentHP * hpUpMultiplier);
        NotifyHealthChanged();
    }

    public bool TakeDamage(float damage)
    {
        EnsureHealthInitialized();

        if (damage <= 0f)
        {
            return false;
        }

        currentHP = Mathf.Max(0f, currentHP - damage);
        NotifyHealthChanged();
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
        NotifyHealthChanged();
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
        NotifyHealthChanged();
    }

    public void SetCurrentMaxHP(float newMaxHP, bool fillHp = false)
    {
        EnsureHealthInitialized();

        currentMaxHP = Mathf.Max(1f, newMaxHP);
        currentHP = fillHp ? currentMaxHP : Mathf.Min(currentHP, currentMaxHP);
        NotifyHealthChanged();
    }


    public void SetHealOnDamagePercent(float percentValue)
    {
        healOnDamagePercent = Mathf.Max(0f, percentValue);
    }

    public void ApplyDealtDamageHeal(float appliedDamage)
    {
        EnsureHealthInitialized();

        if (appliedDamage <= 0f || healOnDamagePercent <= 0f || currentHP <= 0f)
        {
            return;
        }

        Heal(appliedDamage * (healOnDamagePercent * 0.01f));
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

    private void NotifyHealthChanged()
    {
        HealthChanged?.Invoke();
    }

    private void RecalculateCurrentAttack()
    {
        currentAttack = Mathf.Max(0f, baseAttack) * attackUpMultiplier;
    }

    private void RecalculateCurrentPickupRadius()
    {
        currentPickupRadius = Mathf.Max(0f, basePickupRadius) * pickupRadiusMultiplier;
    }
}
