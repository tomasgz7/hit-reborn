using HackSlash.Events;
using UnityEngine;

namespace HackSlash.Feedback
{
    /// <summary>
    /// Shake por "trauma" (técnica de Squirrel Eiserloh): el trauma sube con cada golpe y decae;
    /// el desplazamiento es trauma² × ruido Perlin → los golpes chicos casi no mueven, los grandes sí.
    /// Va en la CÁMARA (hija del rig), así no pelea con el script de seguimiento.
    /// Si usás Cinemachine, reemplazalo por un CinemachineImpulseSource escuchando el mismo canal.
    /// </summary>
    public sealed class CameraShaker : MonoBehaviour
    {
        [SerializeField] private HitEventChannel hitChannel;
        [SerializeField] private float maxOffset = 0.35f;
        [SerializeField] private float maxRoll = 2f;
        [SerializeField] private float frequency = 25f;
        [Tooltip("Trauma que se pierde por segundo.")]
        [SerializeField] private float decay = 2.5f;

        private Vector3 _basePos;
        private Quaternion _baseRot;
        private float _trauma;
        private float _seed;

        private void Awake()
        {
            _basePos = transform.localPosition;
            _baseRot = transform.localRotation;
            _seed = Random.value * 100f;
        }

        private void OnEnable() { if (hitChannel != null) hitChannel.Raised += OnHit; }
        private void OnDisable() { if (hitChannel != null) hitChannel.Raised -= OnHit; }

        private void OnHit(HitResult hit) => AddTrauma(hit.Attack != null ? hit.Attack.CameraShake : 0.2f);

        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        private void LateUpdate()
        {
            if (_trauma <= 0f)
            {
                transform.localPosition = _basePos;
                transform.localRotation = _baseRot;
                return;
            }

            float shake = _trauma * _trauma;
            float time = Time.unscaledTime * frequency;

            float x = (Mathf.PerlinNoise(_seed, time) * 2f - 1f) * maxOffset * shake;
            float y = (Mathf.PerlinNoise(_seed + 1f, time) * 2f - 1f) * maxOffset * shake;
            float roll = (Mathf.PerlinNoise(_seed + 2f, time) * 2f - 1f) * maxRoll * shake;

            transform.localPosition = _basePos + new Vector3(x, y, 0f);
            transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, roll);

            _trauma = Mathf.Max(0f, _trauma - decay * Time.unscaledDeltaTime);
        }
    }
}
