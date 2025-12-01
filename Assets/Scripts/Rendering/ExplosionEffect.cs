using UnityEngine;

namespace RailSim.Rendering
{
    public class ExplosionEffect : MonoBehaviour
    {
        private float _lifetime = 1.5f;
        private float _elapsed;
        private SpriteRenderer[] _particles;
        private Vector3[] _velocities;
        private float[] _rotationSpeeds;
        private const int ParticleCount = 12;

        private static readonly Color[] ExplosionColors =
        {
            new Color(1f, 0.6f, 0.1f),     // Orange
            new Color(1f, 0.3f, 0.1f),     // Red-orange
            new Color(1f, 0.9f, 0.2f),     // Yellow
            new Color(0.4f, 0.4f, 0.4f),   // Smoke gray
            new Color(0.8f, 0.4f, 0.1f),   // Dark orange
        };

        private void Start()
        {
            _particles = new SpriteRenderer[ParticleCount];
            _velocities = new Vector3[ParticleCount];
            _rotationSpeeds = new float[ParticleCount];

            for (var i = 0; i < ParticleCount; i++)
            {
                var go = new GameObject($"Particle_{i}");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale = Vector3.one * Random.Range(0.15f, 0.4f);
                
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PrimitiveSpriteFactory.Square;
                sr.color = ExplosionColors[Random.Range(0, ExplosionColors.Length)];
                sr.sortingOrder = 100;
                
                _particles[i] = sr;
                
                // Random velocity outward
                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var speed = Random.Range(1.5f, 4f);
                _velocities[i] = new Vector3(Mathf.Cos(angle) * speed, Mathf.Sin(angle) * speed, 0f);
                _rotationSpeeds[i] = Random.Range(-360f, 360f);
            }
        }

        private void Update()
        {
            _elapsed += Time.deltaTime;
            
            if (_elapsed >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            var progress = _elapsed / _lifetime;
            var gravity = -3f;

            for (var i = 0; i < ParticleCount; i++)
            {
                if (_particles[i] == null) continue;

                // Apply velocity with gravity
                _velocities[i] += new Vector3(0f, gravity * Time.deltaTime, 0f);
                _particles[i].transform.localPosition += _velocities[i] * Time.deltaTime;
                
                // Rotate
                _particles[i].transform.Rotate(0f, 0f, _rotationSpeeds[i] * Time.deltaTime);
                
                // Fade out and shrink
                var color = _particles[i].color;
                color.a = 1f - progress;
                _particles[i].color = color;
                
                var scale = Mathf.Lerp(1f, 0.2f, progress);
                _particles[i].transform.localScale = Vector3.one * Random.Range(0.15f, 0.4f) * scale;
            }
        }
    }
}

