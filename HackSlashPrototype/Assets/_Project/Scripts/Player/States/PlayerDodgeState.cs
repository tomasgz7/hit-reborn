using HackSlash.Input;
using UnityEngine;

namespace HackSlash.Player.States
{
    /// <summary>
    /// Esquiva con i-frames en una ventana configurable y cancelación a ataque al final
    /// (dodge → attack, clásico del género).
    /// </summary>
    public sealed class PlayerDodgeState : PlayerState
    {
        private readonly AnimationCurve _curve;
        private Vector3 _direction;
        private float _elapsed;
        private float _lastProgress;

        public PlayerDodgeState(PlayerController ctx, AnimationCurve curve) : base(ctx) => _curve = curve;

        public override void Enter()
        {
            _elapsed = 0f;
            _lastProgress = 0f;

            Vector3 input = Ctx.GetMoveDirection();
            _direction = input.sqrMagnitude > 0.01f ? input.normalized : Ctx.transform.forward;
            Ctx.FaceInstant(_direction);

            Ctx.CrossFade(Ctx.DodgeHash, 0.04f);
        }

        public override void Tick(float dt)
        {
            _elapsed += dt;
            float t = _elapsed / Ctx.DodgeDuration;

            float progress = _curve.Evaluate(Mathf.Clamp01(t));
            Ctx.AddMovement(_direction * ((progress - _lastProgress) * Ctx.DodgeDistance));
            _lastProgress = progress;

            Ctx.Health.SetInvulnerable(t >= Ctx.IFrameStart && t <= Ctx.IFrameEnd);

            if (t >= Ctx.DodgeAttackCancel)
            {
                if (Ctx.Buffer.Consume(CombatAction.Light))
                {
                    Ctx.StartAttack(Ctx.Combo.GetLight(0), 0);
                    return;
                }
                if (Ctx.Buffer.Consume(CombatAction.Heavy))
                {
                    Ctx.StartAttack(Ctx.Combo.HeavyStarter, -1);
                    return;
                }
            }

            if (t >= 1f) Ctx.Machine.ChangeState(Ctx.LocomotionState);
        }

        public override void Exit()
        {
            Ctx.Health.SetInvulnerable(false);
            Ctx.MarkDodgeEnded();
        }
    }
}
