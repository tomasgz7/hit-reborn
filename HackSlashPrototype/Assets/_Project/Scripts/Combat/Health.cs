using System;
using UnityEngine;

namespace HackSlash.Combat
{
    /// <summary>
    /// Vida + invulnerabilidad. No sabe nada de animaciones ni de UI:
    /// solo emite eventos y que cada sistema reaccione como quiera.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHealth = 100f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead => Current <= 0f;
        public bool IsInvulnerable { get; private set; }
        public bool CanBeHit => !IsDead && !IsInvulnerable;

        public event Action<DamageInfo> Damaged;
        /// <summary>(actual, máximo) — ideal para barras de vida.</summary>
        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake() => Current = maxHealth;

        public void SetInvulnerable(bool value) => IsInvulnerable = value;

        public void TakeDamage(in DamageInfo info)
        {
            if (!CanBeHit) return;

            Current = Mathf.Max(0f, Current - info.Amount);
            HealthChanged?.Invoke(Current, maxHealth);
            Damaged?.Invoke(info);

            if (IsDead) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            HealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
