using System;

namespace HackSlash.Input
{
    public enum CombatAction { None, Light, Heavy, Dodge }

    /// <summary>
    /// Buffer de input de un solo slot (el último input gana), con vencimiento.
    /// Es lo que hace que el combo se sienta "responsivo": si apretás un poco antes de que
    /// se abra la ventana de combo, el input no se pierde; se ejecuta apenas se puede.
    ///
    /// C# puro: el reloj se inyecta, así se puede testear sin Unity.
    /// </summary>
    public sealed class InputBuffer
    {
        private readonly float _window;
        private readonly Func<float> _clock;

        private CombatAction _action = CombatAction.None;
        private float _timestamp;

        public InputBuffer(float window, Func<float> clock)
        {
            _window = window;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        public void Push(CombatAction action)
        {
            _action = action;
            _timestamp = _clock();
        }

        public bool Peek(out CombatAction action)
        {
            if (_action != CombatAction.None && _clock() - _timestamp > _window)
                _action = CombatAction.None; // venció

            action = _action;
            return action != CombatAction.None;
        }

        public bool Has(CombatAction action) => Peek(out var current) && current == action;

        /// <summary>Si el input bufferizado es 'action', lo consume y devuelve true.</summary>
        public bool Consume(CombatAction action)
        {
            if (!Has(action)) return false;
            _action = CombatAction.None;
            return true;
        }

        public void Clear() => _action = CombatAction.None;
    }
}
