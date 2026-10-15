using UnityEngine;

namespace HackSlash.Combat
{
    /// <summary>
    /// Auto-aim suave estilo mobile ARPG: al iniciar un golpe, el personaje gira hacia el enemigo
    /// más conveniente. Prioriza la dirección del stick; sin stick, elige el mejor alrededor.
    /// Score = mezcla de distancia y ángulo (menor es mejor).
    /// </summary>
    public sealed class TargetAssist : MonoBehaviour
    {
        [SerializeField] private float radius = 4.5f;
        [Tooltip("Con stick: solo considera enemigos dentro de este ángulo respecto al input.")]
        [Range(0, 180)] [SerializeField] private float maxAngleWithInput = 75f;
        [Tooltip("0 = solo importa la distancia, 1 = solo importa el ángulo.")]
        [Range(0, 1)] [SerializeField] private float angleWeight = 0.6f;
        [SerializeField] private LayerMask targetMask;

        private readonly Collider[] _hits = new Collider[24];

        public Transform CurrentTarget { get; private set; }
        public float CurrentTargetDistance { get; private set; }

        /// <summary>Devuelve la dirección hacia donde mirar (plana). Actualiza CurrentTarget.</summary>
        public Vector3 ResolveFacing(Vector3 inputDir)
        {
            bool hasInput = inputDir.sqrMagnitude > 0.01f;
            Vector3 reference = hasInput ? inputDir.normalized : transform.forward;
            float maxAngle = hasInput ? maxAngleWithInput : 180f;

            CurrentTarget = null;
            CurrentTargetDistance = float.MaxValue;

            Vector3 origin = transform.position;
            int count = Physics.OverlapSphereNonAlloc(origin, radius, _hits, targetMask, QueryTriggerInteraction.Ignore);

            float bestScore = float.MaxValue;
            Vector3 bestDir = reference;

            for (int i = 0; i < count; i++)
            {
                var damageable = _hits[i].GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanBeHit) continue;

                Vector3 to = _hits[i].transform.position - origin;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist < 0.01f) continue;

                float angle = Vector3.Angle(reference, to);
                if (angle > maxAngle) continue;

                float score = (dist / radius) * (1f - angleWeight) + (angle / 180f) * angleWeight;
                if (score >= bestScore) continue;

                bestScore = score;
                bestDir = to / dist;
                CurrentTarget = _hits[i].transform;
                CurrentTargetDistance = dist;
            }

            return bestDir;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, radius);
            if (CurrentTarget != null) Gizmos.DrawLine(transform.position, CurrentTarget.position);
        }
    }
}
