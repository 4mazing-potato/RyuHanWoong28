using System.Collections.Generic;
using UnityEngine;

public class SplashDamageController : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";

    [Header("Splash Damage")]
    [Tooltip("1레벨 기준 스플래시 데미지 지름입니다.")]
    [SerializeField] private float baseDiameter = 2.5f;
    [Tooltip("적중한 투사체 데미지에 곱할 스플래시 데미지 비율입니다.")]
    [SerializeField] private float baseDamageRatio = 0.25f;
    [Tooltip("레벨이 오를 때마다 스플래시 데미지와 지름에 곱해지는 배율입니다.")]
    [SerializeField] private float multiplierPerLevel = 1.25f;
    [Tooltip("스플래시 데미지를 받을 대상 레이어입니다. 기본값은 Enemy 레이어입니다.")]
    [SerializeField] private LayerMask affectsLayers;

    private readonly HashSet<int> damagedTargets = new HashSet<int>();
    private PlayerStatus playerStatus;
    private int skillLevel;

    public int SkillLevel => skillLevel;
    public float CurrentDiameter => baseDiameter * Mathf.Pow(multiplierPerLevel, Mathf.Max(0, skillLevel - 1));
    public float CurrentDamageRatio => baseDamageRatio * Mathf.Pow(multiplierPerLevel, Mathf.Max(0, skillLevel - 1));

    public float BaseDiameter
    {
        get => baseDiameter;
        set => baseDiameter = Mathf.Max(0f, value);
    }

    public float BaseDamageRatio
    {
        get => baseDamageRatio;
        set => baseDamageRatio = Mathf.Max(0f, value);
    }

    public LayerMask AffectsLayers
    {
        get => affectsLayers;
        set => affectsLayers = value;
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureDefaultLayerMask();
    }

    private void OnValidate()
    {
        baseDiameter = Mathf.Max(0f, baseDiameter);
        baseDamageRatio = Mathf.Max(0f, baseDamageRatio);
        multiplierPerLevel = Mathf.Max(0f, multiplierPerLevel);
    }

    public void ApplyLevelUpCardValue(float value)
    {
        int newLevel = Mathf.Max(1, Mathf.RoundToInt(value));
        skillLevel = Mathf.Max(skillLevel, newLevel);
    }

    public void ApplySplashDamage(Vector3 hitPosition, float projectileDamage)
    {
        if (GameplayPauseManager.IsPaused || skillLevel <= 0 || projectileDamage <= 0f || CurrentDiameter <= 0f || CurrentDamageRatio <= 0f)
        {
            return;
        }

        CacheReferences();
        ConfigureDefaultLayerMask();

        float splashDamage = projectileDamage * CurrentDamageRatio;
        float splashRadius = CurrentDiameter * 0.5f;
        damagedTargets.Clear();

        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(hitPosition, splashRadius, affectsLayers);
        for (int i = 0; i < hitColliders.Length; i++)
        {
            Collider2D hitCollider = hitColliders[i];
            if (hitCollider == null)
            {
                continue;
            }

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null)
            {
                continue;
            }

            int targetKey = GetDamageableKey(hitCollider, damageable);
            if (!damagedTargets.Add(targetKey))
            {
                continue;
            }

            damageable.TakeDamage(splashDamage, playerStatus);
        }
    }

    private static int GetDamageableKey(Collider2D hitCollider, IDamageable damageable)
    {
        Component damageableComponent = damageable as Component;
        if (damageableComponent != null)
        {
            return damageableComponent.GetInstanceID();
        }

        Rigidbody2D attachedRigidbody = hitCollider.attachedRigidbody;
        return attachedRigidbody != null ? attachedRigidbody.GetInstanceID() : hitCollider.GetInstanceID();
    }

    private void CacheReferences()
    {
        if (playerStatus == null)
        {
            playerStatus = GetComponent<PlayerStatus>();
        }
    }

    private void ConfigureDefaultLayerMask()
    {
        if (affectsLayers.value != 0)
        {
            return;
        }

        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);
        if (enemyLayer >= 0)
        {
            affectsLayers = 1 << enemyLayer;
        }
    }
}
