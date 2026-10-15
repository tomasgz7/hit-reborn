using System;

namespace HackSlash.Core
{
    public interface IState
    {
        void Enter();
        void Tick(float deltaTime);
        void Exit();
    }

    /// <summary>
    /// Máquina de estados genérica, sin dependencias de Unity.
    /// La puede usar el jugador, los enemigos, la UI, etc.
    /// </summary>
    public sealed class StateMachine
    {
        public IState Current { get; private set; }

        /// <summary>(anterior, nuevo). Útil para debug o UI.</summary>
        public event Action<IState, IState> StateChanged;

        public void ChangeState(IState next)
        {
            if (next == null) return;

            IState previous = Current;
            previous?.Exit();
            Current = next;
            Current.Enter();
            StateChanged?.Invoke(previous, next);
        }

        public void Tick(float deltaTime) => Current?.Tick(deltaTime);
    }
}
