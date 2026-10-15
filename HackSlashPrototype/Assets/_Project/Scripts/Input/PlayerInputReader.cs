using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HackSlash.Input
{
    /// <summary>
    /// Traduce el New Input System a eventos de juego. Es la única clase que conoce InputActions:
    /// si mañana el input viene de una IA o de una repetición, se reemplaza esto y nada más cambia.
    /// Compatible con teclado, gamepad y On-Screen Stick/Button (mobile, estilo HIT).
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] private InputActionReference move;
        [SerializeField] private InputActionReference lightAttack;
        [SerializeField] private InputActionReference heavyAttack;
        [SerializeField] private InputActionReference dodge;

        public Vector2 Move => move != null ? Vector2.ClampMagnitude(move.action.ReadValue<Vector2>(), 1f) : Vector2.zero;

        public event Action<CombatAction> ActionPressed;

        private void OnEnable()
        {
            Bind(lightAttack, OnLight);
            Bind(heavyAttack, OnHeavy);
            Bind(dodge, OnDodge);
            move?.action.Enable();
        }

        private void OnDisable()
        {
            Unbind(lightAttack, OnLight);
            Unbind(heavyAttack, OnHeavy);
            Unbind(dodge, OnDodge);
        }

        private void OnLight(InputAction.CallbackContext _) => ActionPressed?.Invoke(CombatAction.Light);
        private void OnHeavy(InputAction.CallbackContext _) => ActionPressed?.Invoke(CombatAction.Heavy);
        private void OnDodge(InputAction.CallbackContext _) => ActionPressed?.Invoke(CombatAction.Dodge);

        private static void Bind(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            if (reference == null) return;
            reference.action.performed += handler;
            reference.action.Enable();
        }

        private static void Unbind(InputActionReference reference, Action<InputAction.CallbackContext> handler)
        {
            if (reference == null) return;
            reference.action.performed -= handler;
        }
    }
}
