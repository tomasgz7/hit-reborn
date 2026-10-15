using UnityEngine;

namespace HackSlash.Combat
{
    /// <summary>
    /// Definición data-driven de UN golpe. Game designers lo tunean sin tocar código.
    /// Todas las ventanas están en tiempo NORMALIZADO (0 = inicio, 1 = fin del golpe),
    /// así si cambiás la duración no hay que recalcular nada.
    /// </summary>
    [CreateAssetMenu(menuName = "HackSlash/Combat/Attack Data", fileName = "ATK_New")]
    public sealed class AttackData : ScriptableObject
    {
        [Header("Animación")]
        [Tooltip("Nombre EXACTO del estado en el Animator Controller (Base Layer).")]
        [SerializeField] private string animatorState = "Light1";
        [Tooltip("Duración del crossfade de entrada. Bajo = más responsivo (0.03–0.08).")]
        [SerializeField] private float crossFade = 0.05f;
        [Tooltip("Duración total del golpe en segundos = largo del clip / speed del estado.")]
        [SerializeField] private float duration = 0.6f;

        [Header("Ventanas (normalizadas 0–1)")]
        [Range(0, 1)] [SerializeField] private float activeStart = 0.30f;
        [Range(0, 1)] [SerializeField] private float activeEnd = 0.45f;
        [Tooltip("Desde acá el input bufferizado encadena el siguiente golpe.")]
        [Range(0, 1)] [SerializeField] private float comboWindowStart = 0.40f;
        [Tooltip("Desde acá se puede cancelar con esquiva. Bajo = combate más permisivo.")]
        [Range(0, 1)] [SerializeField] private float dodgeCancelStart = 0.15f;
        [Tooltip("Desde acá mover el stick corta la recuperación (recovery cancel).")]
        [Range(0, 1)] [SerializeField] private float recoveryStart = 0.75f;

        [Header("Hitbox (espacio local del personaje)")]
        [SerializeField] private Vector3 hitboxCenter = new Vector3(0f, 1f, 1.1f);
        [SerializeField] private Vector3 hitboxSize = new Vector3(2f, 1.5f, 1.8f);

        [Header("Daño")]
        [SerializeField] private float damage = 10f;
        [Tooltip("Velocidad inicial de empuje (m/s).")]
        [SerializeField] private float knockback = 4f;
        [SerializeField] private float hitstun = 0.35f;

        [Header("Game Feel")]
        [SerializeField] private float hitstopAttacker = 0.06f;
        [Tooltip("Normalmente un poco mayor que la del atacante: la víctima 'siente' más el golpe.")]
        [SerializeField] private float hitstopTarget = 0.09f;
        [Range(0, 1)] [SerializeField] private float cameraShake = 0.25f;

        [Header("Desplazamiento (lunge)")]
        [SerializeField] private float lungeDistance = 1.2f;
        [Tooltip("Curva ACUMULADA 0→1. Pendiente fuerte al inicio = embestida explosiva.")]
        [SerializeField] private AnimationCurve lungeCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.25f, 0.85f, 1.5f, 1.5f),
            new Keyframe(0.45f, 1f, 0f, 0f));
        [Tooltip("Si hay un objetivo más cerca que esto, el lunge se acorta para no atravesarlo.")]
        [SerializeField] private float lungeStopDistance = 1.3f;

        private int _stateHash;

        public int StateHash => _stateHash != 0 ? _stateHash : (_stateHash = Animator.StringToHash(animatorState));
        public string AnimatorState => animatorState;
        public float CrossFade => crossFade;
        public float Duration => duration;
        public float ActiveStart => activeStart;
        public float ActiveEnd => activeEnd;
        public float ComboWindowStart => comboWindowStart;
        public float DodgeCancelStart => dodgeCancelStart;
        public float RecoveryStart => recoveryStart;
        public Vector3 HitboxCenter => hitboxCenter;
        public Vector3 HitboxSize => hitboxSize;
        public float Damage => damage;
        public float Knockback => knockback;
        public float Hitstun => hitstun;
        public float HitstopAttacker => hitstopAttacker;
        public float HitstopTarget => hitstopTarget;
        public float CameraShake => cameraShake;
        public float LungeDistance => lungeDistance;
        public float LungeStopDistance => lungeStopDistance;

        /// <summary>Progreso acumulado del lunge (0–1) en el tiempo normalizado t.</summary>
        public float EvaluateLunge(float t) => lungeCurve.Evaluate(Mathf.Clamp01(t));

        private void OnValidate()
        {
            _stateHash = 0;
            duration = Mathf.Max(0.05f, duration);
            activeEnd = Mathf.Max(activeStart, activeEnd);
        }
    }
}
