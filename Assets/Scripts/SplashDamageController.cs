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
    [Tooltip("스플래시 데미지가 발동할 때 명중 위치에 생성할 이펙트 프리팹입니다.")]
    [SerializeField] private GameObject splashEffectPrefab;
    [Tooltip("이펙트 프리팹을 자동으로 제거할 시간입니다. 0 이하이면 애니메이션 길이를 사용합니다.")]
    [SerializeField] private float effectLifetime = 0.35f;
    [Tooltip("이펙트의 X/Y 스케일을 현재 스플래시 지름에 맞춥니다.")]
    [SerializeField] private bool scaleEffectToDiameter = true;

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

    public GameObject SplashEffectPrefab
    {
        get => splashEffectPrefab;
        set => splashEffectPrefab = value;
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
        effectLifetime = Mathf.Max(0f, effectLifetime);
    }

    public void ApplyLevelUpCardValue(float value)
    {
        int newLevel = Mathf.Max(1, Mathf.RoundToInt(value));
        skillLevel = Mathf.Max(skillLevel, newLevel);
    }

    public void ApplySplashDamage(Vector3 hitPosition, float projectileDamage, Collider2D directHitCollider = null, IDamageable directHitDamageable = null)
    {
        if (GameplayPauseManager.IsPaused || skillLevel <= 0 || projectileDamage <= 0f || CurrentDiameter <= 0f || CurrentDamageRatio <= 0f)
        {
            return;
        }

        CacheReferences();
        ConfigureDefaultLayerMask();

        float splashDamage = projectileDamage * CurrentDamageRatio;
        float currentDiameter = CurrentDiameter;
        float splashRadius = currentDiameter * 0.5f;
        int excludedTargetKey = GetExcludedTargetKey(directHitCollider, directHitDamageable);

        SpawnSplashEffect(hitPosition, currentDiameter);
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
            if (targetKey == excludedTargetKey || !damagedTargets.Add(targetKey))
            {
                continue;
            }

            damageable.TakeDamage(splashDamage, playerStatus);
        }
    }

    private void SpawnSplashEffect(Vector3 hitPosition, float currentDiameter)
    {
        if (splashEffectPrefab == null)
        {
            Debug.LogWarning($"{nameof(SplashDamageController)} on {name} requires a splash effect prefab assigned in the Inspector.", this);
            return;
        }

        GameObject effectObject = Instantiate(splashEffectPrefab, hitPosition, Quaternion.identity);
        if (scaleEffectToDiameter)
        {
            ScaleEffectToDiameter(effectObject, currentDiameter);
        }

        float destroyDelay = effectLifetime > 0f ? effectLifetime : GetAnimationLength(effectObject);
        Destroy(effectObject, destroyDelay);
    }

    private static void ScaleEffectToDiameter(GameObject effectObject, float currentDiameter)
    {
        if (currentDiameter <= 0f)
        {
            return;
        }

        SpriteRenderer spriteRenderer = effectObject.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            return;
        }

        float currentEffectDiameter = Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        if (currentEffectDiameter <= 0f)
        {
            return;
        }

        float scaleMultiplier = currentDiameter / currentEffectDiameter;
        effectObject.transform.localScale *= scaleMultiplier;
    }

    private static float GetAnimationLength(GameObject effectObject)
    {
        Animator animator = effectObject.GetComponent<Animator>();
        if (animator != null && animator.runtimeAnimatorController != null && animator.runtimeAnimatorController.animationClips.Length > 0)
        {
            return animator.runtimeAnimatorController.animationClips[0].length;
        }

        return 0.35f;
    }

    private static int GetExcludedTargetKey(Collider2D directHitCollider, IDamageable directHitDamageable)
    {
        if (directHitDamageable != null)
        {
            return GetDamageableKey(directHitCollider, directHitDamageable);
        }

        return 0;
    }

    private static int GetDamageableKey(Collider2D hitCollider, IDamageable damageable)
    {
        Component damageableComponent = damageable as Component;
        if (damageableComponent != null)
        {
            return damageableComponent.GetInstanceID();
        }

        if (hitCollider == null)
        {
            return 0;
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
