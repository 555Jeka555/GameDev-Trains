using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class FinishView : MonoBehaviour
    {
        private SpriteRenderer _baseRenderer;
        private SpriteRenderer _poleRenderer;
        private SpriteRenderer _flagRenderer;
        private SpriteRenderer _flagStripeRenderer;
        private Transform _flagPivot;
        private CircleCollider2D _collider;
        private TextMesh _label;
        private RailNode _node;
        private RailGameController _controller;
        private float _pulseTimer;
        private float _waveTimer;
        private float _cachedSize;

        private void Awake()
        {
            _baseRenderer = GetComponent<SpriteRenderer>();
            _baseRenderer.sprite = PrimitiveSpriteFactory.Square;
            _baseRenderer.drawMode = SpriteDrawMode.Sliced;
            _baseRenderer.sortingOrder = 4;
            _baseRenderer.color = new Color(0.2f, 0.6f, 0.2f, 0.7f);

            // Tall pole - dark wood color
            _poleRenderer = CreateChildRenderer("Pole", 5, new Color(0.4f, 0.25f, 0.15f, 1f));
            
            // Flag pivot for wave animation
            var pivotGo = new GameObject("FlagPivot");
            pivotGo.transform.SetParent(transform, false);
            _flagPivot = pivotGo.transform;
            
            // Main flag - bright checkered racing flag style
            _flagRenderer = CreateChildRenderer("Flag", 6, new Color(1f, 0.1f, 0.1f, 1f), _flagPivot);
            
            // White stripe on flag
            _flagStripeRenderer = CreateChildRenderer("FlagStripe", 7, new Color(1f, 1f, 1f, 0.95f), _flagPivot);

            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = 0.45f;

            _label = CreateLabel();
        }

        private SpriteRenderer CreateChildRenderer(string name, int sortingOrder, Color color, Transform parent = null)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent ?? transform, false);
            child.transform.localPosition = Vector3.zero;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = PrimitiveSpriteFactory.Square;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.sortingOrder = sortingOrder;
            renderer.color = color;
            return renderer;
        }

        private TextMesh CreateLabel()
        {
            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            var mesh = go.AddComponent<TextMesh>();
            mesh.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mesh.fontSize = 64;
            mesh.characterSize = 0.12f;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            mesh.text = "🏁 ФИНИШ";
            mesh.color = new Color(1f, 0.95f, 0.3f);
            var renderer = mesh.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 10;
            }
            return mesh;
        }

        private void Update()
        {
            if (_flagRenderer == null || _flagPivot == null)
            {
                return;
            }

            _pulseTimer += Time.deltaTime;
            _waveTimer += Time.deltaTime * 4f;
            
            // Wave animation - flag swaying in wind
            var waveAngle = Mathf.Sin(_waveTimer) * 8f + Mathf.Sin(_waveTimer * 1.7f) * 4f;
            _flagPivot.localRotation = Quaternion.Euler(0f, 0f, waveAngle);
            
            // Slight scale pulse for wind effect
            var scaleX = 1f + Mathf.Sin(_waveTimer * 1.3f) * 0.05f;
            _flagRenderer.transform.localScale = new Vector3(scaleX, 1f, 1f);
            _flagStripeRenderer.transform.localScale = new Vector3(scaleX, 1f, 1f);

            // Pulse the base platform
            var basePulse = 0.7f + 0.3f * Mathf.Sin(_pulseTimer * 2.5f);
            var baseColor = _baseRenderer.color;
            baseColor.a = basePulse * 0.7f;
            _baseRenderer.color = baseColor;

            if (_label != null)
            {
                var labelColor = _label.color;
                labelColor.a = 0.85f + 0.15f * Mathf.Sin(_pulseTimer * 2f);
                _label.color = labelColor;
            }
        }

        public void Bind(RailNode node, RailGameController controller, float size)
        {
            _node = node;
            _controller = controller;
            SetSize(size);
        }

        public void SetSize(float size)
        {
            _cachedSize = Mathf.Max(0.3f, size);
            var s = _cachedSize;
            
            // Base platform
            _baseRenderer.size = new Vector2(s * 1.2f, s * 0.35f);
            
            // Tall pole
            var poleHeight = s * 2.5f;
            var poleWidth = s * 0.12f;
            _poleRenderer.size = new Vector2(poleWidth, poleHeight);
            _poleRenderer.transform.localPosition = new Vector3(-s * 0.4f, poleHeight * 0.5f, 0f);
            
            // Flag pivot at top of pole
            _flagPivot.localPosition = new Vector3(-s * 0.4f, poleHeight - s * 0.3f, 0f);
            
            // Large waving flag
            var flagWidth = s * 1.2f;
            var flagHeight = s * 0.7f;
            _flagRenderer.size = new Vector2(flagWidth, flagHeight);
            _flagRenderer.transform.localPosition = new Vector3(flagWidth * 0.5f + poleWidth * 0.5f, 0f, 0f);
            
            // White stripe across flag
            _flagStripeRenderer.size = new Vector2(flagWidth, flagHeight * 0.3f);
            _flagStripeRenderer.transform.localPosition = new Vector3(flagWidth * 0.5f + poleWidth * 0.5f, 0f, 0f);

            if (_label != null)
            {
                _label.characterSize = s * 0.1f;
                _label.transform.localPosition = new Vector3(s * 0.3f, poleHeight + s * 0.15f, 0f);
            }

            if (_collider != null)
            {
                _collider.radius = s * 0.9f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_controller == null || _node == null)
            {
                return;
            }

            if (other.TryGetComponent<TrainView>(out var trainView))
            {
                _controller.HandleFinishContact(_node, trainView);
            }
        }
    }
}

