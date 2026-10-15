using HackSlash.Combat;
using HackSlash.Core;
using UnityEngine;

namespace HackSlash.Enemy
{
    /// <summary>
    /// Feedback de impacto en la víctima — el 50% del "game feel" está acá:
    ///  • Pose de golpe instantánea (Animator.Play desde 0)
    ///  • Flash blanco durante el hitstop
    ///  • Temblor del modelo mientras está congelado
    ///  • Knockback que arranca recién cuando termina el hitstop
    /// Sirve tal cual para un "training dummy". Para una IA real se reemplaza por un estado HitReact.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class EnemyHitReaction : MonoBehaviour
    {
        [Header("Referencias")]
        [SerializeField] private Animator animator;
        [SerializeField] private HitstopController hitstop;
        [Tooltip("Hijo con el modelo (NO el root con el collider). Es lo que tiembla.")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private Renderer[] renderers;

        [Header("Animator")]
        [SerializeField] private string hitState = "Hit";
        [SerializeField] private string deathState = "Death";

        [Header("Flash")]
        [Tooltip("URP Lit: _BaseColor · Built-in Standard: _Color")]
        [SerializeField] private string colorProperty = "_BaseColor";
        [SerializeField] private Color flashColor = Color.white;
        [SerializeField] private float flashDuration = 0.08f;

        [Header("Temblor durante hitstop")]
        [SerializeField] private float shakeAmplitude = 0.06f;

        [Header("Knockback")]
        [SerializeField] private float knockbackDamping = 8f;
        [SerializeField] private bool faceAttacker = true;

        private Health _health;
        private CharacterController _cc;
        private MaterialPropertyBlock _mpb;
        private int _colorId, _hitHash, _deathHash;
        private Vector3 _visualBasePos;
        private Vector3 _knockVelocity;
        private float _flashTimer;
        private bool _flashActive;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _cc = GetComponent<CharacterController>();
            _mpb = new MaterialPropertyBlock();
            _colorId = Shader.PropertyToID(colorProperty);
            _hitHash = Animator.StringToHash(hitState);
            _deathHash = Animator.StringToHash(deathState);
            if (visualRoot != null) _visualBasePos = visualRoot.localPosition;
            if (animator != null) animator.applyRootMotion = false;
        }

        private void OnEnable()
        {
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        private void OnDamaged(DamageInfo info)
        {
            if (faceAttacker && info.Direction.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(-info.Direction);

            if (!_health.IsDead && animator != null) animator.Play(_hitHash, 0, 0f);

            _knockVelocity = info.Direction * info.KnockbackForce;
            SetFlash(true);
            _flashTimer = flashDuration;
        }

        private void OnDied()
        {
            if (animator != null) animator.CrossFadeInFixedTime(_deathHash, 0.05f, 0, 0f);
        }

        private void Update()
        {
            bool frozen = hitstop != null && hitstop.IsFrozen;

            // Flash: dura al menos flashDuration y nunca se apaga mientras siga el hitstop
            if (_flashActive)
            {
                _flashTimer -= Time.unscaledDeltaTime;
                if (_flashTimer <= 0f && !frozen) SetFlash(false);
            }

            // Temblor
            if (visualRoot != null)
            {
                if (frozen)
                {
                    Vector2 r = Random.insideUnitCircle * shakeAmplitude;
                    visualRoot.localPosition = _visualBasePos + new Vector3(r.x, 0f, r.y);
                }
                else visualRoot.localPosition = _visualBasePos;
            }

            // Knockback (solo cuando no está congelado)
            if (frozen || _knockVelocity.sqrMagnitude < 0.0001f) return;

            float dt = Time.deltaTime;
            Vector3 delta = _knockVelocity * dt;
            if (_cc != null && _cc.enabled) _cc.Move(delta);
            else transform.position += delta;
            _knockVelocity *= Mathf.Exp(-knockbackDamping * dt);
        }

        private void SetFlash(bool on)
        {
            _flashActive = on;
            foreach (Renderer r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                if (on) _mpb.SetColor(_colorId, flashColor);
                else _mpb.Clear();
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}
