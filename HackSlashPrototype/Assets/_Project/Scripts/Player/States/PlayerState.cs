using HackSlash.Core;

namespace HackSlash.Player.States
{
    /// <summary>Base de todos los estados del jugador: solo guarda el contexto.</summary>
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController Ctx;

        protected PlayerState(PlayerController ctx) => Ctx = ctx;

        public virtual void Enter() { }
        public virtual void Tick(float deltaTime) { }
        public virtual void Exit() { }
    }
}
