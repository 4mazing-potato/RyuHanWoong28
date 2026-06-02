using System;
using UnityEngine;

public interface IDamageable
{
    float TakeDamage(float damage, PlayerStatus attackerStatus);
}

public interface IProjectileDamageable : IDamageable
{
}

public static class MonsterDeathEvent
{
    public static event Action<PlayerStatus, Vector3> PlayerKilledMonster;

    public static void RaisePlayerKilledMonster(PlayerStatus attackerStatus, Vector3 deathPosition)
    {
        if (attackerStatus == null)
        {
            return;
        }

        PlayerKilledMonster?.Invoke(attackerStatus, deathPosition);
    }
}

public static class DamageConfirmedEvent
{
    public static event Action<PlayerStatus, float> PlayerDamageDealt;

    public static void RaisePlayerDamageDealt(PlayerStatus attackerStatus, float appliedDamage)
    {
        if (attackerStatus == null || appliedDamage <= 0f)
        {
            return;
        }

        PlayerDamageDealt?.Invoke(attackerStatus, appliedDamage);
        attackerStatus.ApplyDealtDamageHeal(appliedDamage);
    }
}

public class Projectile : ProjectileController
{
}
