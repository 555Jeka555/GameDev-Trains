using RailSim.Core;
using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class TrainView : MonoBehaviour
    {
        private SpriteRenderer _bodyRenderer;
        private SpriteRenderer _cabinRenderer;
        private SpriteRenderer _windowRenderer;
        private SpriteRenderer _frontRenderer;
        private Rigidbody2D _rigidbody;
        private CircleCollider2D _collider;
        private TrainRuntime _runtime;

        public TrainRuntime Runtime => _runtime;

        private void Awake()
        {
            // Main body
            _bodyRenderer = GetComponent<SpriteRenderer>();
            _bodyRenderer.sprite = PrimitiveSpriteFactory.Square;
            _bodyRenderer.drawMode = SpriteDrawMode.Sliced;
            _bodyRenderer.size = new Vector2(1.0f, 0.4f);
            _bodyRenderer.sortingOrder = 5;

            // Cabin (raised part)
            _cabinRenderer = CreatePart("Cabin", 6);
            _cabinRenderer.size = new Vector2(0.5f, 0.25f);
            _cabinRenderer.transform.localPosition = new Vector3(-0.15f, 0.15f, 0f);

            // Window
            _windowRenderer = CreatePart("Window", 7);
            _windowRenderer.size = new Vector2(0.2f, 0.12f);
            _windowRenderer.transform.localPosition = new Vector3(-0.15f, 0.18f, 0f);
            _windowRenderer.color = new Color(0.7f, 0.85f, 1f, 0.9f);

            // Front (locomotive nose)
            _frontRenderer = CreatePart("Front", 6);
            _frontRenderer.size = new Vector2(0.15f, 0.32f);
            _frontRenderer.transform.localPosition = new Vector3(0.45f, 0f, 0f);

            _rigidbody = GetComponent<Rigidbody2D>();
            _rigidbody.isKinematic = true;
            _rigidbody.gravityScale = 0f;

            _collider = GetComponent<CircleCollider2D>();
            _collider.isTrigger = true;
            _collider.radius = 0.4f;
        }

        private SpriteRenderer CreatePart(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrimitiveSpriteFactory.Square;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = order;
            return sr;
        }

        // Predefined colors for different train indices
        private static readonly Color[] TrainColors =
        {
            new(0.15f, 0.45f, 0.85f, 1f),  // Blue
            new(0.9f, 0.5f, 0.1f, 1f),     // Orange
            new(0.2f, 0.75f, 0.3f, 1f),    // Green
            new(0.85f, 0.2f, 0.25f, 1f),   // Red
            new(0.7f, 0.3f, 0.8f, 1f),     // Purple
            new(0.95f, 0.8f, 0.1f, 1f),    // Yellow
            new(0.1f, 0.7f, 0.7f, 1f),     // Cyan
            new(0.9f, 0.4f, 0.6f, 1f),     // Pink
        };

        private static int _trainColorIndex;

        public void Bind(TrainRuntime runtime)
        {
            _runtime = runtime;
            
            Color mainColor;
            
            // Try to parse custom color from hex
            if (!string.IsNullOrEmpty(runtime.Blueprint.colorHex) && 
                ColorUtility.TryParseHtmlString("#" + runtime.Blueprint.colorHex, out var customColor))
            {
                mainColor = customColor;
            }
            else
            {
                // Use train index-based color for variety
                mainColor = TrainColors[_trainColorIndex % TrainColors.Length];
                _trainColorIndex++;
            }
            
            // Create accent color (darker version)
            Color.RGBToHSV(mainColor, out var h, out var s, out var v);
            var accentColor = Color.HSVToRGB(h, Mathf.Min(1f, s * 1.2f), v * 0.8f);
            
            _bodyRenderer.color = mainColor;
            _cabinRenderer.color = accentColor;
            _frontRenderer.color = accentColor;
        }

        public static void ResetColorIndex() => _trainColorIndex = 0;

        private void LateUpdate()
        {
            if (_runtime == null)
            {
                return;
            }

            transform.position = _runtime.GetWorldPosition();
            var direction = _runtime.GetDirection();
            if (direction.sqrMagnitude > 0.001f)
            {
                var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }
}

