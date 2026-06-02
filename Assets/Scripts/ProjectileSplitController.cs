using UnityEngine;

public class ProjectileSplitController : MonoBehaviour
{
    [Header("Projectile Split")]
    [Tooltip("분열된 투사체가 원래 투사체 데미지에서 가지는 비율입니다.")]
    [SerializeField] private float splitDamageRatio = 0.25f;
    [Tooltip("분열 투사체가 생성 지점에서 겹친 콜라이더에 즉시 재충돌하지 않도록 이동시키는 거리입니다.")]
    [SerializeField] private float spawnOffset = 0.1f;

    private int skillLevel;

    public int SkillLevel => skillLevel;
    public int CurrentSplitCount => skillLevel > 0 ? skillLevel + 1 : 0;
    public float SplitDamageRatio
    {
        get => splitDamageRatio;
        set => splitDamageRatio = Mathf.Max(0f, value);
    }

    private void OnValidate()
    {
        splitDamageRatio = Mathf.Max(0f, splitDamageRatio);
        spawnOffset = Mathf.Max(0f, spawnOffset);
    }

    public void ApplyLevelUpCardValue(float value)
    {
        int newLevel = Mathf.Max(1, Mathf.RoundToInt(value));
        skillLevel = Mathf.Max(skillLevel, newLevel);
    }

    public void SplitProjectile(ProjectileController projectilePrefab, Vector3 splitPosition, PlayerStatus ownerStatus, float damageMultiplier, float speed, float lifetime, float scale)
    {
        int splitCount = CurrentSplitCount;
        if (projectilePrefab == null || ownerStatus == null || splitCount <= 0 || splitDamageRatio <= 0f)
        {
            return;
        }

        float angleStep = 360f / splitCount;
        float splitDamageMultiplier = damageMultiplier * splitDamageRatio;
        for (int i = 0; i < splitCount; i++)
        {
            float angle = angleStep * i;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector3 spawnPosition = splitPosition + (Vector3)(direction * spawnOffset);
            ProjectileController splitProjectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            splitProjectile.Initialize(direction, ownerStatus, splitDamageMultiplier, speed, lifetime, scale, false);
        }
    }
}
