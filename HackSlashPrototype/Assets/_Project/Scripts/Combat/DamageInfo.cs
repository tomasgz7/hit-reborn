using UnityEngine;

namespace HackSlash.Combat
{
    /// <summary>Paquete inmutable con todo lo que la víctima necesita saber de un golpe.</summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 HitPoint;
        /// <summary>Dirección atacante → víctima, normalizada y plana (y = 0).</summary>
        public readonly Vector3 Direction;
        /// <summary>Velocidad inicial de empuje en m/s.</summary>
        public readonly float KnockbackForce;
        public readonly float HitstunDuration;
        public readonly GameObject Source;

        public DamageInfo(float amount, Vector3 hitPoint, Vector3 direction,
                          float knockbackForce, float hitstunDuration, GameObject source)
        {
            Amount = amount;
            HitPoint = hitPoint;
            Direction = direction;
            KnockbackForce = knockbackForce;
            HitstunDuration = hitstunDuration;
            Source = source;
        }
    }

    /// <summary>Cualquier cosa que pueda recibir un golpe: jugador, enemigo, barril, etc.</summary>
    public interface IDamageable
    {
        bool CanBeHit { get; }
        void TakeDamage(in DamageInfo info);
    }

    /// <summary>Lo que el Hitbox reporta cuando toca algo válido.</summary>
    public readonly struct HitContact
    {
        public readonly IDamageable Target;
        public readonly Collider Collider;
        public readonly Vector3 Point;

        public HitContact(IDamageable target, Collider collider, Vector3 point)
        {
            Target = target;
            Collider = collider;
            Point = point;
        }
    }
}
