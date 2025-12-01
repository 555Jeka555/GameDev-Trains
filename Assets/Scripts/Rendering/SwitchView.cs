using System.Collections;
using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class SwitchView : MonoBehaviour
    {
        private static readonly Vector3 DefaultScale = Vector3.one * 0.45f;

        private SpriteRenderer _renderer;
        private SpriteRenderer _indicatorRenderer;
        private RailSwitchState _state;
        private RailNode _node;
        private RailGraph _graph;
        private Coroutine _pulseRoutine;
        private Vector3 _baseScale = DefaultScale;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = PrimitiveSpriteFactory.Square;
            _renderer.sortingOrder = 2;
            transform.localScale = _baseScale;

            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;

            _indicatorRenderer = CreateIndicatorRenderer();
        }

        private SpriteRenderer CreateIndicatorRenderer()
        {
            var go = new GameObject("Indicator");
            go.transform.SetParent(transform, false);
            var indicator = go.AddComponent<SpriteRenderer>();
            indicator.sprite = PrimitiveSpriteFactory.Square;
            indicator.drawMode = SpriteDrawMode.Sliced;
            indicator.sortingOrder = 3;
            indicator.color = new Color(1f, 0.95f, 0.35f, 0.95f);
            indicator.size = new Vector2(0.15f, 0.7f);
            return indicator;
        }

        public void Initialize(RailNode node, RailGraph graph)
        {
            _node = node;
            _graph = graph;
        }

        public void Bind(RailSwitchState state)
        {
            _state = state;
            UpdateVisual();
            UpdateIndicator();
        }

        public void SetVisualSize(float size)
        {
            var clamped = Mathf.Max(0.2f, size);
            _baseScale = Vector3.one * clamped;
            transform.localScale = _baseScale;
            if (_indicatorRenderer != null)
            {
                _indicatorRenderer.size = new Vector2(clamped * 0.2f, clamped * 1.2f);
            }
        }

        public void Pulse()
        {
            if (_pulseRoutine != null)
            {
                StopCoroutine(_pulseRoutine);
            }

            _pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            const float duration = 0.15f;
            var elapsed = 0f;
            var targetScale = _baseScale * 1.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(_baseScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                transform.localScale = Vector3.Lerp(targetScale, _baseScale, t);
                yield return null;
            }

            transform.localScale = _baseScale;
            _pulseRoutine = null;
        }

        public void UpdateVisual()
        {
            if (_state == null)
            {
                return;
            }

            var hue = Mathf.Abs(_state.NodeId.GetHashCode() % 360) / 360f;
            _renderer.color = Color.HSVToRGB(hue, 0.5f, 0.95f);
            UpdateIndicator();
        }

        public void UpdateIndicator()
        {
            if (_indicatorRenderer == null || _state == null || _graph == null || _node == null)
            {
                return;
            }

            var targetId = _state.CurrentTarget;
            if (string.IsNullOrEmpty(targetId))
            {
                _indicatorRenderer.enabled = false;
                return;
            }

            var targetNode = _graph.GetNode(targetId);
            if (targetNode == null)
            {
                _indicatorRenderer.enabled = false;
                return;
            }

            _indicatorRenderer.enabled = true;
            var direction = (targetNode.WorldPosition - _node.WorldPosition).normalized;
            var offset = direction * (_indicatorRenderer.size.y * 0.3f);
            _indicatorRenderer.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
            _indicatorRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        public string NodeId => _state?.NodeId;
    }
}

