using System;
using UnityEngine;

namespace HackSlash.Core
{
    /// <summary>
    /// Hitstop LOCAL (por entidad): congela el Animator de este personaje unos milisegundos.
    /// No toca Time.timeScale, así que el resto del mundo (partículas, otros enemigos, UI) sigue vivo,
    /// que es lo que hacen la mayoría de los hack and slash modernos.
    ///
    /// Cualquier lógica que dependa del tiempo (timers de ataque, knockback) debe consultar IsFrozen.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitstopController : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        [Tooltip("Tope de seguridad: ningún hitstop acumulado dura más que esto.")]
        [SerializeField] private float maxDuration = 0.25f;

        private float _remaining;
        private float _savedSpeed = 1f;

        public bool IsFrozen => _remaining > 0f;

        /// <summary>true al congelar, false al liberar.</summary>
        public event Action<bool> FrozenChanged;

        private void Reset() => animator = GetComponentInChildren<Animator>();

        /// <summary>
        /// Aplica hitstop. Si ya está congelado, NO suma: se queda con el mayor restante.
        /// Así, pegarle a 5 enemigos en el mismo frame no genera una pausa de 5x.
        /// </summary>
        public void Apply(float duration)
        {
            if (duration <= 0f) return;

            if (!IsFrozen)
            {
                _savedSpeed = animator.speed;
                animator.speed = 0f;
                FrozenChanged?.Invoke(true);
            }

            _remaining = Mathf.Min(Mathf.Max(_remaining, duration), maxDuration);
        }

        private void Update()
        {
            if (!IsFrozen) return;

            // Unscaled: un slow-motion global no debe estirar el hitstop.
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining > 0f) return;

            _remaining = 0f;
            animator.speed = _savedSpeed;
            FrozenChanged?.Invoke(false);
        }

        private void OnDisable()
        {
            if (!IsFrozen) return;
            _remaining = 0f;
            if (animator != null) animator.speed = _savedSpeed;
        }
    }
}
