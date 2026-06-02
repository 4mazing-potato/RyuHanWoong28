using System.Collections.Generic;
using UnityEngine;

public class ExplosionController : MonoBehaviour
{
    private const string EnemyLayerName = "Enemy";

    [Header("Explosion")]
    [Tooltip("처치된 적 위치에서 발생하는 원형 폭발 범위입니다.")]
    [SerializeField] private float explosionRadius = 2.5f;
    [Tooltip("폭발 피해를 받을 대상 레이어입니다. 기본값은 Enemy 레이어입니다.")]
    [SerializeField] private LayerMask affectsLayers;
    [Tooltip("PlayerStatus.CurrentAttack에 곱할 퍼센트 배율입니다. 예: 30 = 현재 공격력의 30% 피해")]
    [SerializeField] private float damageMultiplier = 30f;
    [Tooltip("폭발 위치에 잠깐 표시할 링 이펙트 프리팹입니다.")]
    [SerializeField] private GameObject effectsPrefab;

    [Header("Activation")]
    [Tooltip("적 사망 시 폭발이 발생할 확률입니다. 예: 10 = 10% 확률")]
    [SerializeField] private float triggerChancePercent;

    private PlayerStatus playerStatus;

    public float ExplosionRadius
    {
        get => explosionRadius;
        set => explosionRadius = Mathf.Max(0f, value);
    }

    public LayerMask AffectsLayers
    {
        get => affectsLayers;
        set => affectsLayers = value;
    }

    public float DamageMultiplier
    {
        get => damageMultiplier;
        set => damageMultiplier = Mathf.Max(0f, value);
    }

    public GameObject EffectsPrefab
    {
        get => effectsPrefab;
        set => effectsPrefab = value;
    }

    public float TriggerChancePercent
    {
        get => triggerChancePercent;
        set => triggerChancePercent = Mathf.Clamp(value, 0f, 100f);
    }

    private void Awake()
    {
        CacheReferences();
        ConfigureDefaultLayerMask();
    }

    private void OnEnable()
    {
        MonsterDeathEvent.PlayerKilledMonster += TryExplodeAtKilledMonster;
    }

    private void OnDisable()
    {
        MonsterDeathEvent.PlayerKilledMonster -= TryExplodeAtKilledMonster;
    }

    private void OnValidate()
    {
        explosionRadius = Mathf.Max(0f, explosionRadius);
        damageMultiplier = Mathf.Max(0f, damageMultiplier);
        triggerChancePercent = Mathf.Clamp(triggerChancePercent, 0f, 100f);
    }

    public void ApplyLevelUpCardValue(float value)
    {
        TriggerChancePercent = value;
    }

    private void TryExplodeAtKilledMonster(PlayerStatus attackerStatus, Vector3 deathPosition)
    {
        if (GameplayPauseManager.IsPaused || attackerStatus == null || attackerStatus != GetPlayerStatus())
        {
            return;
        }

        if (explosionRadius <= 0f || triggerChancePercent <= 0f || Random.value > triggerChancePercent * 0.01f)
        {
            return;
        }

        SpawnEffect(deathPosition);
        DamageEnemiesInRadius(deathPosition);
    }

    private void DamageEnemiesInRadius(Vector3 explosionPosition)
    {
        PlayerStatus status = GetPlayerStatus();
        float damage = status != null ? status.CalculateDamage(damageMultiplier * 0.01f) : damageMultiplier * 0.01f;
        if (damage <= 0f)
        {
            return;
        }

        HashSet<int> damagedTargets = new HashSet<int>();
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(explosionPosition, explosionRadius, affectsLayers);
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

            damageable.TakeDamage(damage, status);
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

    private void SpawnEffect(Vector3 explosionPosition)
    {
        if (effectsPrefab == null)
        {
            return;
        }

        GameObject effectObject = Instantiate(effectsPrefab, explosionPosition, Quaternion.identity);
        ExplosionEffectController effectController = effectObject.GetComponent<ExplosionEffectController>();
        if (effectController != null)
        {
            effectController.Play(explosionRadius * 2f);
        }
        else
        {
            effectObject.transform.localScale = Vector3.one * explosionRadius;
        }
    }

    private PlayerStatus GetPlayerStatus()
    {
        CacheReferences();
        return playerStatus;
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
