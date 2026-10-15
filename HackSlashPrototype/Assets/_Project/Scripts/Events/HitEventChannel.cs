using System;
using HackSlash.Combat;
using UnityEngine;

namespace HackSlash.Events
{
    public readonly struct HitResult
    {
        public readonly Transform Attacker;
        public readonly Collider Target;
        public readonly Vector3 Point;
        public readonly AttackData Attack;

        public HitResult(Transform attacker, Collider target, Vector3 point, AttackData attack)
        {
            Attacker = attacker;
            Target = target;
            Point = point;
            Attack = attack;
        }
    }

    /// <summary>
    /// Canal de eventos como asset (patrón "ScriptableObject Event Channel").
    /// El combate publica; cámara, VFX, audio, UI de combo, vibración... se suscriben.
    /// Ninguno conoce al otro: se arrastra el mismo asset en el Inspector y listo.
    /// </summary>
    [CreateAssetMenu(menuName = "HackSlash/Events/Hit Event Channel", fileName = "EVT_PlayerHit")]
    public sealed class HitEventChannel : ScriptableObject
    {
        public event Action<HitResult> Raised;

        public void Raise(in HitResult result) => Raised?.Invoke(result);
    }
}
