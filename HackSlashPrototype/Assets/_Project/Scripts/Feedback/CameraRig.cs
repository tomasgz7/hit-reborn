using UnityEngine;

namespace HackSlash.Feedback
{
    /// <summary>
    /// Cámara isométrica/3ra persona fija estilo HIT: sigue al jugador con suavizado exponencial
    /// (independiente del framerate) y ángulo constante. La Camera va como HIJA con el CameraShaker.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 9f, -7.5f);
        [SerializeField] private float lookHeight = 1f;
        [Tooltip("Más alto = sigue más pegado.")]
        [SerializeField] private float followSharpness = 10f;

        private void Start()
        {
            if (target == null) return;
            transform.position = target.position + offset;
            transform.rotation = Quaternion.LookRotation(-offset + Vector3.up * lookHeight);
        }

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 desired = target.position + offset;
            float k = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, desired, k);
        }
    }
}
