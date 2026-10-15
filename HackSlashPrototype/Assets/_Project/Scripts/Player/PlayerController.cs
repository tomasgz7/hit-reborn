using HackSlash.Combat;
using HackSlash.Core;
using HackSlash.Events;
using HackSlash.Input;
using HackSlash.Player.States;
using UnityEngine;

namespace HackSlash.Player
{
    /// <summary>
    /// "Contexto" del State Pattern: expone referencias y utilidades (mover, rotar, animar)
    /// pero NO decide nada. Toda la lógica de "qué puedo hacer ahora" vive en los estados.
    ///
    /// Movimiento: todo pasa por AddMovement() y se aplica UNA vez por frame con gravedad,
    /// así ningún estado pelea con otro por el CharacterController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Hitbox hitbox;
        [SerializeField] private Health health;
        [SerializeField] private HitstopController hitstop;
        [SerializeField] private TargetAssist targetAssist;
        [Tooltip("Para que el movimiento sea relativo a cámara. Si está vacío usa Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Datos")]
        [SerializeField] private ComboData combo;
        [SerializeField] private HitEventChannel hitChannel;

        [Header("Locomoción")]
        [SerializeField] private float moveSpeed = 6f;
        [Tooltip("Grados por segundo.")]
        [SerializeField] private float turnSpeed = 900f;
        [SerializeField] private float gravity = -30f;
        [SerializeField] private float speedParamDamp = 0.08f;

        [Header("Esquiva")]
        [SerializeField] private float dodgeDistance = 5f;
        [SerializeField] private float dodgeDuration = 0.4f;
        [SerializeField] private AnimationCurve dodgeCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 2.5f, 2.5f), new Keyframe(1f, 1f, 0f, 0f));
        [Range(0, 1)] [SerializeField] private float iFrameStart = 0.05f;
        [Range(0, 1)] [SerializeField] private float iFrameEnd = 0.7f;
        [Tooltip("Desde acá la esquiva se puede cancelar en ataque (dodge-attack).")]
        [Range(0, 1)] [SerializeField] private float dodgeAttackCancel = 0.6f;
        [SerializeField] private float dodgeCooldown = 0.15f;

        [Header("Input")]
        [Tooltip("Cuánto tiempo 'vive' un input apretado antes de tiempo.")]
        [SerializeField] private float bufferWindow = 0.3f;

        [Header("Animator (nombres de estados/parámetros)")]
        [SerializeField] private string locomotionState = "Locomotion";
        [SerializeField] private string dodgeState = "Dodge";
        [SerializeField] private string hitState = "Hit";
        [SerializeField] private string deathState = "Death";
        [SerializeField] private string speedParam = "Speed";

        private CharacterController _cc;
        private Vector3 _pendingMove;
        private float _verticalVelocity;
        private float _lastDodgeEndTime = -999f;

        // ---------- API pública para los estados ----------
        public StateMachine Machine { get; } = new StateMachine();
        public InputBuffer Buffer { get; private set; }

        public PlayerLocomotionState LocomotionState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerDodgeState DodgeState { get; private set; }
        public PlayerHitStunState HitStunState { get; private set; }
        public PlayerDeadState DeadState { get; private set; }

        public Animator Animator => animator;
        public Hitbox Hitbox => hitbox;
        public Health Health => health;
        public HitstopController Hitstop => hitstop;
        public TargetAssist TargetAssist => targetAssist;
        public ComboData Combo => combo;
        public HitEventChannel HitChannel => hitChannel;

        public float MoveSpeed => moveSpeed;
        public float SpeedParamDamp => speedParamDamp;
        public float DodgeDistance => dodgeDistance;
        public float DodgeDuration => dodgeDuration;
        public float IFrameStart => iFrameStart;
        public float IFrameEnd => iFrameEnd;
        public float DodgeAttackCancel => dodgeAttackCancel;
        public bool CanDodge => Time.time - _lastDodgeEndTime >= dodgeCooldown;

        public int LocomotionHash { get; private set; }
        public int DodgeHash { get; private set; }
        public int HitHash { get; private set; }
        public int DeathHash { get; private set; }
        public int SpeedHash { get; private set; }

        public bool HasMoveInput => input.Move.sqrMagnitude > 0.04f;

        // ---------- Ciclo de vida ----------
        private void Awake()
        {
            _cc = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
            animator.applyRootMotion = false; // el desplazamiento lo controla el código (data-driven)

            LocomotionHash = Animator.StringToHash(locomotionState);
            DodgeHash = Animator.StringToHash(dodgeState);
            HitHash = Animator.StringToHash(hitState);
            DeathHash = Animator.StringToHash(deathState);
            SpeedHash = Animator.StringToHash(speedParam);

            Buffer = new InputBuffer(bufferWindow, () => Time.time);

            LocomotionState = new PlayerLocomotionState(this);
            AttackState = new PlayerAttackState(this);
            DodgeState = new PlayerDodgeState(this, dodgeCurve);
            HitStunState = new PlayerHitStunState(this);
            DeadState = new PlayerDeadState(this);
        }

        private void OnEnable()
        {
            input.ActionPressed += Buffer.Push;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        private void OnDisable()
        {
            input.ActionPressed -= Buffer.Push;
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        private void Start() => Machine.ChangeState(LocomotionState);

        private void Update()
        {
            float dt = Time.deltaTime;
            Machine.Tick(dt);
            ApplyMovement(dt);
        }

        // ---------- Utilidades para los estados ----------
        public void AddMovement(Vector3 horizontalDelta)
        {
            horizontalDelta.y = 0f;
            _pendingMove += horizontalDelta;
        }

        /// <summary>Input del stick convertido a dirección de mundo relativa a la cámara.</summary>
        public Vector3 GetMoveDirection()
        {
            Vector2 raw = input.Move;
            if (raw.sqrMagnitude < 0.0001f) return Vector3.zero;

            Vector3 forward = cameraTransform != null ? cameraTransform.forward : Vector3.forward;
            Vector3 right = cameraTransform != null ? cameraTransform.right : Vector3.right;
            forward.y = 0f; right.y = 0f;
            forward.Normalize(); right.Normalize();

            return Vector3.ClampMagnitude(forward * raw.y + right * raw.x, 1f);
        }

        public void RotateTowards(Vector3 direction, float dt)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
        }

        public void FaceInstant(Vector3 direction)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>Crossfade arrancando el estado destino desde 0 (clave para combos).</summary>
        public void CrossFade(int stateHash, float duration) =>
            animator.CrossFadeInFixedTime(stateHash, duration, 0, 0f);

        public void StartAttack(AttackData data, int lightIndex)
        {
            if (data == null) return;
            AttackState.Setup(data, lightIndex);
            Machine.ChangeState(AttackState);
        }

        public void MarkDodgeEnded() => _lastDodgeEndTime = Time.time;

        // ---------- Internos ----------
        private void ApplyMovement(float dt)
        {
            if (_cc.isGrounded && _verticalVelocity < 0f) _verticalVelocity = -2f;
            else _verticalVelocity += gravity * dt;

            _pendingMove.y = _verticalVelocity * dt;
            _cc.Move(_pendingMove);
            _pendingMove = Vector3.zero;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (health.IsDead) return;
            HitStunState.Setup(info);
            Machine.ChangeState(HitStunState); // re-entra si ya estaba en hitstun: reinicia el stun
        }

        private void OnDied() => Machine.ChangeState(DeadState);
    }
}
