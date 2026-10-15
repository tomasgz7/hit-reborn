using HackSlash.Combat;
using UnityEngine;

namespace HackSlash.Player.States
{
    /// <summary>El jugador recibió daño: animación de golpe, empuje y sin control por un rato.</summary>
    public sealed class PlayerHitStunState : PlayerState
    {
        private const float KnockbackDamping = 8f;

        private DamageInfo _info;
        private Vector3 _knockVelocity;
        private float _elapsed;

        public PlayerHitStunState(PlayerController ctx) : base(ctx) { }

        public void Setup(in DamageInfo info) => _info = info;

        public override void Enter()
        {
            _elapsed = 0f;
            Ctx.Buffer.Clear(); // no ejecutar inputs viejos al salir del stun

            _knockVelocity = _info.Direction * _info.KnockbackForce;
            Ctx.FaceInstant(-_info.Direction); // mirar al atacante
            Ctx.Animator.Play(Ctx.HitHash, 0, 0f); // Play (no CrossFade): la reacción debe ser instantánea
        }

        public override void Tick(float dt)
        {
            if (Ctx.Hitstop.IsFrozen) return;

            _elapsed += dt;
            Ctx.AddMovement(_knockVelocity * dt);
            _knockVelocity *= Mathf.Exp(-KnockbackDamping * dt);

            if (_elapsed >= _info.HitstunDuration) Ctx.Machine.ChangeState(Ctx.LocomotionState);
        }
    }

    public sealed class PlayerDeadState : PlayerState
    {
        public PlayerDeadState(PlayerController ctx) : base(ctx) { }

        public override void Enter()
        {
            Ctx.Buffer.Clear();
            Ctx.CrossFade(Ctx.DeathHash, 0.08f);
        }
    }
}
