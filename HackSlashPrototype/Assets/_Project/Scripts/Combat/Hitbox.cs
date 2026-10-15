using System;
using System.Collections.Generic;
using UnityEngine;

namespace HackSlash.Combat
{
    /// <summary>
    /// Hitbox por consulta física (OverlapBox), no por triggers.
    /// Ventajas: no depende del orden de OnTriggerEnter, es determinista, y el tamaño/posición
    /// viene de cada AttackData (un golpe giratorio puede tener un box enorme, un estoque uno largo y fino).
    ///
    /// No aplica daño: solo REPORTA contactos. Quién decide qué hacer es el estado de ataque.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Hitbox : MonoBehaviour
    {
        [SerializeField] private LayerMask targetMask;
        [Tooltip("La Health del dueño, para no pegarse a sí mismo.")]
        [SerializeField] private Health owner;
        [Tooltip("Solo para previsualizar el box en la Scene view.")]
        [SerializeField] private AttackData gizmoPreview;

        private readonly Collider[] _results = new Collider[16];
        private readonly HashSet<IDamageable> _hitThisSwing = new HashSet<IDamageable>();

        private AttackData _current;
        private bool _active;

        public event Action<HitContact> Hit;

        /// <summary>Empieza un golpe nuevo: resetea la lista de "ya golpeados".</summary>
        public void BeginSwing(AttackData data)
        {
            _current = data;
            _active = false;
            _hitThisSwing.Clear();
        }

        public void SetActive(bool value)
        {
            if (_active == value) return;
            _active = value;
            if (_active) Scan(); // escaneo inmediato: no perder golpes en frames largos
        }

        public void EndSwing()
        {
            _active = false;
            _current = null;
        }

        private void Update()
        {
            if (_active) Scan();
        }

        private void Scan()
        {
            if (_current == null) return;

            Vector3 center = transform.TransformPoint(_current.HitboxCenter);
            int count = Physics.OverlapBoxNonAlloc(
                center, _current.HitboxSize * 0.5f, _results, transform.rotation,
                targetMask, QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider col = _results[i];
                var target = col.GetComponentInParent<IDamageable>();

                if (target == null) continue;
                if (ReferenceEquals(target, owner)) continue;
                if (!target.CanBeHit) continue;
                if (!_hitThisSwing.Add(target)) continue; // un golpe = un impacto por víctima

                Hit?.Invoke(new HitContact(target, col, col.ClosestPoint(center)));
            }
        }

        private void OnDrawGizmos()
        {
            AttackData data = _current != null ? _current : gizmoPreview;
            if (data == null) return;

            Gizmos.color = _active ? new Color(1f, 0.15f, 0.1f, 0.45f) : new Color(1f, 0.8f, 0.1f, 0.2f);
            Gizmos.matrix = Matrix4x4.TRS(transform.TransformPoint(data.HitboxCenter), transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, data.HitboxSize);
            Gizmos.DrawWireCube(Vector3.zero, data.HitboxSize);
        }
    }
}
