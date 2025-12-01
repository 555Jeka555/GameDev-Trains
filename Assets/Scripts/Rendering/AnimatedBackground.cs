using UnityEngine;

namespace RailSim.Rendering
{
    public class AnimatedBackground : MonoBehaviour
    {
        public enum BackgroundTheme
        {
            Forest,
            Desert,
            Snow,
            Night,
            Sunset
        }

        private SpriteRenderer _bgRenderer;
        private readonly ParticleRenderer[] _particles = new ParticleRenderer[25];
        private float _time;
        private BackgroundTheme _currentTheme;

        private static AnimatedBackground _instance;
        public static AnimatedBackground Instance => _instance;

        // Theme colors
        private struct ThemeColors
        {
            public Color SkyTop;
            public Color SkyBottom;
            public Color Ground;
            public Color Particle1;
            public Color Particle2;
            public string Name;
        }

        private static readonly ThemeColors[] Themes =
        {
            new ThemeColors // Forest
            {
                SkyTop = new Color(0.12f, 0.15f, 0.25f, 1f),
                SkyBottom = new Color(0.25f, 0.45f, 0.55f, 1f),
                Ground = new Color(0.2f, 0.45f, 0.15f, 1f),
                Particle1 = new Color(1f, 1f, 1f, 0.3f),
                Particle2 = new Color(0.8f, 1f, 0.8f, 0.2f),
                Name = "Лес"
            },
            new ThemeColors // Desert
            {
                SkyTop = new Color(0.95f, 0.85f, 0.6f, 1f),
                SkyBottom = new Color(0.9f, 0.7f, 0.4f, 1f),
                Ground = new Color(0.85f, 0.7f, 0.45f, 1f),
                Particle1 = new Color(0.9f, 0.85f, 0.7f, 0.2f),
                Particle2 = new Color(1f, 0.95f, 0.8f, 0.15f),
                Name = "Пустыня"
            },
            new ThemeColors // Snow
            {
                SkyTop = new Color(0.5f, 0.6f, 0.75f, 1f),
                SkyBottom = new Color(0.75f, 0.85f, 0.95f, 1f),
                Ground = new Color(0.9f, 0.95f, 1f, 1f),
                Particle1 = new Color(1f, 1f, 1f, 0.6f),
                Particle2 = new Color(0.95f, 0.98f, 1f, 0.5f),
                Name = "Снег"
            },
            new ThemeColors // Night
            {
                SkyTop = new Color(0.02f, 0.02f, 0.08f, 1f),
                SkyBottom = new Color(0.08f, 0.1f, 0.2f, 1f),
                Ground = new Color(0.05f, 0.08f, 0.05f, 1f),
                Particle1 = new Color(1f, 1f, 0.8f, 0.7f),
                Particle2 = new Color(0.8f, 0.9f, 1f, 0.5f),
                Name = "Ночь"
            },
            new ThemeColors // Sunset
            {
                SkyTop = new Color(0.15f, 0.1f, 0.25f, 1f),
                SkyBottom = new Color(0.95f, 0.5f, 0.3f, 1f),
                Ground = new Color(0.15f, 0.2f, 0.1f, 1f),
                Particle1 = new Color(1f, 0.7f, 0.4f, 0.3f),
                Particle2 = new Color(1f, 0.5f, 0.3f, 0.2f),
                Name = "Закат"
            }
        };

        public static AnimatedBackground Create()
        {
            if (_instance != null)
            {
                Destroy(_instance.gameObject);
            }
            
            var go = new GameObject("AnimatedBackground");
            var bg = go.AddComponent<AnimatedBackground>();
            bg.Initialize();
            _instance = bg;
            return bg;
        }

        private void Initialize()
        {
            // Load saved theme
            var savedTheme = PlayerPrefs.GetInt("BackgroundTheme", 0);
            _currentTheme = (BackgroundTheme)Mathf.Clamp(savedTheme, 0, Themes.Length - 1);

            // Main background
            _bgRenderer = gameObject.AddComponent<SpriteRenderer>();
            _bgRenderer.sprite = PrimitiveSpriteFactory.Square;
            _bgRenderer.drawMode = SpriteDrawMode.Sliced;
            _bgRenderer.size = new Vector2(100f, 100f);
            _bgRenderer.sortingOrder = -100;

            CreateGrassLayer();
            CreateHills();
            
            for (var i = 0; i < _particles.Length; i++)
            {
                _particles[i] = CreateParticle(i);
            }

            ApplyTheme(_currentTheme);
        }

        public void SetTheme(BackgroundTheme theme)
        {
            _currentTheme = theme;
            PlayerPrefs.SetInt("BackgroundTheme", (int)theme);
            PlayerPrefs.Save();
            ApplyTheme(theme);
        }

        public BackgroundTheme CurrentTheme => _currentTheme;
        public static string[] GetThemeNames()
        {
            var names = new string[Themes.Length];
            for (var i = 0; i < Themes.Length; i++)
            {
                names[i] = Themes[i].Name;
            }
            return names;
        }

        private void ApplyTheme(BackgroundTheme theme)
        {
            var colors = Themes[(int)theme];
            
            _bgRenderer.color = colors.SkyBottom;

            // Update ground color
            var ground = transform.Find("Grass");
            if (ground != null)
            {
                ground.GetComponent<SpriteRenderer>().color = colors.Ground;
            }

            // Update hills
            for (var i = 0; i < 5; i++)
            {
                var hill = transform.Find($"Hill_{i}");
                if (hill != null)
                {
                    var sr = hill.GetComponent<SpriteRenderer>();
                    sr.color = new Color(
                        colors.Ground.r * Random.Range(0.85f, 1.1f),
                        colors.Ground.g * Random.Range(0.85f, 1.05f),
                        colors.Ground.b * Random.Range(0.9f, 1.1f),
                        1f
                    );
                }
            }

            // Update particles
            var isNight = theme == BackgroundTheme.Night;
            var isSnow = theme == BackgroundTheme.Snow;
            
            foreach (var particle in _particles)
            {
                if (particle?.Renderer == null) continue;

                if (particle.IsStar || isNight)
                {
                    particle.Renderer.color = colors.Particle1;
                    if (isNight)
                    {
                        particle.Renderer.size = new Vector2(0.2f, 0.2f);
                        particle.IsStar = true;
                    }
                }
                else
                {
                    particle.Renderer.color = Random.value > 0.5f ? colors.Particle1 : colors.Particle2;
                    if (isSnow)
                    {
                        particle.Renderer.size = new Vector2(Random.Range(0.15f, 0.4f), Random.Range(0.15f, 0.4f));
                        particle.Speed = Random.Range(0.3f, 0.8f);
                    }
                }
            }
        }

        private void CreateGrassLayer()
        {
            var grass = new GameObject("Grass");
            grass.transform.SetParent(transform, false);
            grass.transform.localPosition = new Vector3(0f, -35f, 0f);
            var sr = grass.AddComponent<SpriteRenderer>();
            sr.sprite = PrimitiveSpriteFactory.Square;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(120f, 30f);
            sr.sortingOrder = -99;
        }

        private void CreateHills()
        {
            for (var i = 0; i < 5; i++)
            {
                var hill = new GameObject($"Hill_{i}");
                hill.transform.SetParent(transform, false);
                var x = (i - 2) * 25f + Random.Range(-5f, 5f);
                var y = -25f + Random.Range(-3f, 3f);
                hill.transform.localPosition = new Vector3(x, y, 0f);
                
                var sr = hill.AddComponent<SpriteRenderer>();
                sr.sprite = PrimitiveSpriteFactory.Square;
                sr.drawMode = SpriteDrawMode.Sliced;
                var size = Random.Range(15f, 25f);
                sr.size = new Vector2(size, size * 0.6f);
                sr.sortingOrder = -98;
            }
        }

        private ParticleRenderer CreateParticle(int index)
        {
            var go = new GameObject($"Particle_{index}");
            go.transform.SetParent(transform, false);
            
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrimitiveSpriteFactory.Square;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = -95;

            var particle = new ParticleRenderer
            {
                Renderer = sr,
                Transform = go.transform,
                Speed = Random.Range(0.1f, 0.4f),
                Phase = Random.Range(0f, Mathf.PI * 2f),
                BaseY = Random.Range(5f, 40f),
                Amplitude = Random.Range(1f, 3f)
            };

            particle.Transform.localPosition = new Vector3(
                Random.Range(-50f, 50f),
                particle.BaseY,
                0f
            );

            var isCloud = Random.value > 0.3f;
            if (isCloud)
            {
                sr.size = new Vector2(Random.Range(3f, 8f), Random.Range(1.5f, 3f));
            }
            else
            {
                sr.size = new Vector2(0.3f, 0.3f);
                particle.IsStar = true;
            }

            return particle;
        }

        private void Update()
        {
            _time += Time.deltaTime;

            var isSnow = _currentTheme == BackgroundTheme.Snow;

            foreach (var particle in _particles)
            {
                if (particle?.Transform == null) continue;

                var pos = particle.Transform.localPosition;
                
                pos.x += particle.Speed * Time.deltaTime;
                if (pos.x > 55f) pos.x = -55f;
                
                if (isSnow && !particle.IsStar)
                {
                    // Snow falls down
                    pos.y -= particle.Speed * 2f * Time.deltaTime;
                    if (pos.y < -20f)
                    {
                        pos.y = 50f;
                        pos.x = Random.Range(-50f, 50f);
                    }
                }
                else
                {
                    pos.y = particle.BaseY + Mathf.Sin(_time * 0.5f + particle.Phase) * particle.Amplitude;
                }
                
                particle.Transform.localPosition = pos;

                if (particle.IsStar)
                {
                    var alpha = 0.4f + 0.3f * Mathf.Sin(_time * 3f + particle.Phase);
                    var color = particle.Renderer.color;
                    color.a = alpha;
                    particle.Renderer.color = color;
                }
            }
        }

        private class ParticleRenderer
        {
            public SpriteRenderer Renderer;
            public Transform Transform;
            public float Speed;
            public float Phase;
            public float BaseY;
            public float Amplitude;
            public bool IsStar;
        }
    }
}
