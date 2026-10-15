using HackSlash.Input;
using UnityEngine;

namespace HackSlash.Player.States
{
    /// <summary>Idle/Run. Desde acá se puede salir a cualquier acción.</summary>
    public sealed class PlayerLocomotionState : PlayerState
    {
        public PlayerLocomotionState(PlayerController ctx) : base(ctx) { }

        public override void Enter() => Ctx.CrossFade(Ctx.LocomotionHash, 0.12f);

        public override void Tick(float dt)
        {
            // Prioridad: esquiva > ligero > pesado
            if (Ctx.CanDodge && Ctx.Buffer.Consume(CombatAction.Dodge))
            {
                Ctx.Machine.ChangeState(Ctx.DodgeState);
                return;
            }
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

            Vector3 dir = Ctx.GetMoveDirection();
            Ctx.AddMovement(dir * (Ctx.MoveSpeed * dt));
            Ctx.RotateTowards(dir, dt);
            Ctx.Animator.SetFloat(Ctx.SpeedHash, dir.magnitude, Ctx.SpeedParamDamp, dt);
        }
    }
}
