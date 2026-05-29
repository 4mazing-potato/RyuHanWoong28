using UnityEngine;

public class PlayerStatus : MonoBehaviour
{
    [Header("Attack")]
    [SerializeField] private float baseAttack = 1f;
    [SerializeField] private float currentAttack = 1f;

    private float attackUpMultiplier = 1f;

    public float BaseAttack => baseAttack;
    public float CurrentAttack => currentAttack;

    private void Awake()
    {
        RecalculateCurrentAttack();
    }

    private void OnValidate()
    {
        baseAttack = Mathf.Max(0f, baseAttack);
        RecalculateCurrentAttack();
    }

    public void ApplyAttackUpPercent(float percentValue)
    {
        attackUpMultiplier = Mathf.Max(0f, percentValue) * 0.01f;
        RecalculateCurrentAttack();
    }

    public float CalculateDamage(float damageMultiplier)
    {
        return currentAttack * Mathf.Max(0f, damageMultiplier);
    }

    private void RecalculateCurrentAttack()
    {
        currentAttack = Mathf.Max(0f, baseAttack) * attackUpMultiplier;
    }
}
