using HackSlash.Combat;
using HackSlash.Core;
using HackSlash.Events;
using HackSlash.Input;
using UnityEngine;

namespace HackSlash.Player.States
{
    /// <summary>
    /// Ejecuta un AttackData. Línea de tiempo de un golpe (t normalizado):
    ///
    ///  0 ──── dodgeCancel ──── active[start..end] ── comboWindow ──── recovery ──── 1
    ///          ↑ esquivar          ↑ hitbox ON          ↑ encadenar        ↑ moverse
    ///
    /// El reloj del golpe se PAUSA durante el hitstop, así el timing de las ventanas
    /// queda igual aunque congelemos la animación.
    /// </summary>
    public sealed class PlayerAttackState : PlayerState
    {
        private AttackData _data;
        private int _lightIndex;      // -1 = no viene de la cadena ligera (pesado)
        private float _elapsed;
        private float _lastLunge;
        private float _lungeScale;

        public PlayerAttackState(PlayerController ctx) : base(ctx) { }

        /// <summary>Llamar antes de ChangeState.</summary>
        public void Setup(AttackData data, int lightIndex)
        {
            _data = data;
            _lightIndex = lightIndex;
        }

        public override void Enter()
        {
            _elapsed = 0f;
            _lastLunge = 0f;

            // 1) Auto-aim: girar hacia el mejor objetivo (o hacia el stick).
            Vector3 input = Ctx.GetMoveDirection();
            Vector3 facing = Ctx.TargetAssist != null ? Ctx.TargetAssist.ResolveFacing(input) : input;
            Ctx.FaceInstant(facing);

            // 2) Acortar el lunge si el objetivo ya está encima (no atravesarlo).
            _lungeScale = 1f;
            if (Ctx.TargetAssist != null && Ctx.TargetAssist.CurrentTarget != null && _data.LungeDistance > 0f)
            {
                float room = Ctx.TargetAssist.CurrentTargetDistance - _data.LungeStopDistance;
                _lungeScale = Mathf.Clamp01(room / _data.LungeDistance);
            }

            // 3) Animación + hitbox.
            Ctx.CrossFade(_data.StateHash, _data.CrossFade);
            Ctx.Animator.SetFloat(Ctx.SpeedHash, 0f);
            Ctx.Hitbox.Hit += OnHit;
            Ctx.Hitbox.BeginSwing(_data);
        }

        public override void Tick(float dt)
        {
            if (Ctx.Hitstop.IsFrozen) return; // el input se sigue bufferizando igual

            _elapsed += dt;
            float t = _elapsed / _data.Duration;

            // Lunge (curva acumulada → delta de este frame)
            float lunge = _data.EvaluateLunge(t);
            Ctx.AddMovement(Ctx.transform.forward * ((lunge - _lastLunge) * _data.LungeDistance * _lungeScale));
            _lastLunge = lunge;

            // Ventana activa del hitbox
            Ctx.Hitbox.SetActive(t >= _data.ActiveStart && t <= _data.ActiveEnd);

            // Cancelaciones, por prioridad
            if (t >= _data.DodgeCancelStart && Ctx.CanDodge && Ctx.Buffer.Consume(CombatAction.Dodge))
            {
                Ctx.Machine.ChangeState(Ctx.DodgeState);
                return;
            }

            if (t >= _data.ComboWindowStart && TryChain()) return;

            if (t >= _data.RecoveryStart && Ctx.HasMoveInput)
            {
                Ctx.Machine.ChangeState(Ctx.LocomotionState);
                return;
            }

            if (t >= 1f) Ctx.Machine.ChangeState(Ctx.LocomotionState);
        }

        public override void Exit()
        {
            Ctx.Hitbox.Hit -= OnHit;
            Ctx.Hitbox.EndSwing();
        }

        private bool TryChain()
        {
            if (!Ctx.Buffer.Peek(out CombatAction action)) return false;

            switch (action)
            {
                case CombatAction.Light:
                {
                    // Si venimos de un pesado, el ligero reinicia la cadena.
                    int next = _lightIndex >= 0 ? _lightIndex + 1 : 0;
                    AttackData nextData = Ctx.Combo.GetLight(next);
                    if (nextData == null) return false; // fin de cadena: el input sigue en buffer y
                                                         // se usará al volver a Locomotion
                    Ctx.Buffer.Consume(CombatAction.Light);
                    Ctx.StartAttack(nextData, next);
                    return true;
                }
                case CombatAction.Heavy:
                {
                    AttackData branch = Ctx.Combo.GetHeavyBranch(_lightIndex);
                    if (branch == null) return false;
                    Ctx.Buffer.Consume(CombatAction.Heavy);
                    Ctx.StartAttack(branch, -1);
                    return true;
                }
                default:
                    return false;
            }
        }

        private void OnHit(HitContact contact)
        {
            Vector3 dir = contact.Collider.transform.position - Ctx.transform.position;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Ctx.transform.forward;

            // Hitstop PRIMERO: así la víctima arranca su reacción ya congelada en el pose de impacto.
            Ctx.Hitstop.Apply(_data.HitstopAttacker);
            var targetHitstop = contact.Collider.GetComponentInParent<HitstopController>();
            if (targetHitstop != null) targetHitstop.Apply(_data.HitstopTarget);

            contact.Target.TakeDamage(new DamageInfo(
                _data.Damage, contact.Point, dir, _data.Knockback, _data.Hitstun, Ctx.gameObject));

            if (Ctx.HitChannel != null)
                Ctx.HitChannel.Raise(new HitResult(Ctx.transform, contact.Collider, contact.Point, _data));
        }
    }
}
