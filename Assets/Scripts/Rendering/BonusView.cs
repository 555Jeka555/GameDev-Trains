using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    public class BonusView : MonoBehaviour
    {
        private SpriteRenderer _glowRenderer;
        private SpriteRenderer _starRenderer;
        private SpriteRenderer _coreRenderer;
        private BonusRuntime _bonus;
        private float _time;
        private bool _collected;

        private static readonly Color StarColor = new(1f, 0.9f, 0.2f, 1f);
        private static readonly Color GlowColor = new(1f, 0.95f, 0.5f, 0.4f);

        public BonusRuntime Bonus => _bonus;

        public void Initialize(BonusRuntime bonus)
        {
            _bonus = bonus;
            transform.position = bonus.WorldPosition;
            CreateVisuals();
        }

        private void CreateVisuals()
        {
            // Outer glow
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            _glowRenderer = glowGo.AddComponent<SpriteRenderer>();
            _glowRenderer.sprite = PrimitiveSpriteFactory.Square;
            _glowRenderer.sortingOrder = 8;
            _glowRenderer.color = GlowColor;
            glowGo.transform.localScale = Vector3.one * 0.8f;

            // Core circle
            var coreGo = new GameObject("Core");
            coreGo.transform.SetParent(transform, false);
            _coreRenderer = coreGo.AddComponent<SpriteRenderer>();
            _coreRenderer.sprite = PrimitiveSpriteFactory.Square;
            _coreRenderer.sortingOrder = 9;
            _coreRenderer.color = new Color(0.3f, 0.25f, 0.1f, 1f);
            coreGo.transform.localScale = Vector3.one * 0.4f;

            // Star icon
            var starGo = new GameObject("Star");
            starGo.transform.SetParent(transform, false);
            _starRenderer = starGo.AddComponent<SpriteRenderer>();
            _starRenderer.sprite = PrimitiveSpriteFactory.Square;
            _starRenderer.sortingOrder = 10;
            _starRenderer.color = StarColor;
            starGo.transform.localScale = Vector3.one * 0.25f;
            starGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);

            // Add second star rotated for 8-point star effect
            var star2Go = new GameObject("Star2");
            star2Go.transform.SetParent(transform, false);
            var star2Sr = star2Go.AddComponent<SpriteRenderer>();
            star2Sr.sprite = PrimitiveSpriteFactory.Square;
            star2Sr.sortingOrder = 10;
            star2Sr.color = StarColor;
            star2Go.transform.localScale = Vector3.one * 0.25f;
        }

        private void Update()
        {
            if (_collected) return;
            if (_bonus != null && _bonus.IsCollected && !_collected)
            {
                PlayCollectAnimation();
                return;
            }

            _time += Time.deltaTime;

            // Floating animation
            var yOffset = Mathf.Sin(_time * 3f) * 0.1f;
            transform.position = _bonus.WorldPosition + new Vector3(0f, yOffset, 0f);

            // Glow pulse
            var glowScale = 0.8f + 0.2f * Mathf.Sin(_time * 4f);
            if (_glowRenderer != null)
            {
                _glowRenderer.transform.localScale = Vector3.one * glowScale;
            }

            // Rotation
            if (_starRenderer != null)
            {
                _starRenderer.transform.Rotate(0f, 0f, 60f * Time.deltaTime);
            }
        }

        private void PlayCollectAnimation()
        {
            _collected = true;
            StartCoroutine(CollectRoutine());
        }

        private System.Collections.IEnumerator CollectRoutine()
        {
            var elapsed = 0f;
            const float duration = 0.4f;
            var startScale = transform.localScale;
            var startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / duration;

                // Scale up and fade
                transform.localScale = startScale * (1f + t * 2f);
                transform.position = startPos + Vector3.up * t * 0.5f;

                // Fade all renderers
                var alpha = 1f - t;
                if (_glowRenderer != null)
                {
                    var c = _glowRenderer.color;
                    c.a = alpha * GlowColor.a;
                    _glowRenderer.color = c;
                }
                if (_starRenderer != null)
                {
                    var c = _starRenderer.color;
                    c.a = alpha;
                    _starRenderer.color = c;
                }
                if (_coreRenderer != null)
                {
                    var c = _coreRenderer.color;
                    c.a = alpha;
                    _coreRenderer.color = c;
                }

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}

