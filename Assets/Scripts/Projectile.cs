using System;

public interface IDamageable
{
    float TakeDamage(float damage, PlayerStatus attackerStatus);
}

public interface IProjectileDamageable : IDamageable
{
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
