using HackSlash.Events;
using UnityEngine;
using UnityEngine.Pool;

namespace HackSlash.Feedback
{
    /// <summary>
    /// Spawnea un VFX de impacto en el punto de contacto, con pooling (UnityEngine.Pool)
    /// para no generar basura en combos largos. El prefab necesita un ParticleSystem en el root
    /// con Looping OFF y Play On Awake OFF; vuelve al pool pasado 'lifetime'.
    /// </summary>
    public sealed class HitVfxSpawner : MonoBehaviour
    {
        [SerializeField] private HitEventChannel hitChannel;
        [SerializeField] private ParticleSystem hitVfxPrefab;
        [SerializeField] private float lifetime = 0.6f;

        private ObjectPool<ParticleSystem> _pool;

        private void Awake()
        {
            _pool = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(hitVfxPrefab, transform),
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => ps.gameObject.SetActive(false),
                actionOnDestroy: ps => Destroy(ps.gameObject),
                defaultCapacity: 8, maxSize: 32);
        }

        private void OnEnable() { if (hitChannel != null) hitChannel.Raised += OnHit; }
        private void OnDisable() { if (hitChannel != null) hitChannel.Raised -= OnHit; }

        private void OnHit(HitResult hit)
        {
            if (hitVfxPrefab == null) return;

            ParticleSystem ps = _pool.Get();
            Vector3 dir = hit.Point - hit.Attacker.position;
            dir.y = 0f;
            ps.transform.SetPositionAndRotation(hit.Point,
                dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity);
            ps.Play(true);
            StartCoroutine(ReleaseAfter(ps));
        }

        private System.Collections.IEnumerator ReleaseAfter(ParticleSystem ps)
        {
            yield return new WaitForSecondsRealtime(lifetime);
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _pool.Release(ps);
        }
    }
}
