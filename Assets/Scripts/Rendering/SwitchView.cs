using System.Collections;
using System.Collections.Generic;
using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Collider2D))]
    public class SwitchView : MonoBehaviour
    {
        private static readonly Vector3 DefaultScale = Vector3.one * 0.5f;

        private SpriteRenderer _baseRenderer;
        private RailSwitchState _state;
        private RailNode _node;
        private RailGraph _graph;
        private Coroutine _pulseRoutine;
        private Vector3 _baseScale = DefaultScale;
        private float _animationTime;

        // Track rail visuals
        private readonly List<LineRenderer> _railLines = new();
        private readonly List<SpriteRenderer> _railEnds = new();
        private LineRenderer _activeRailHighlight;
        
        // Colors
        private static readonly Color RailColor = new(0.4f, 0.42f, 0.45f, 1f);
        private static readonly Color ActiveRailColor = new(0.3f, 0.85f, 0.4f, 1f);
        private static readonly Color InactiveRailColor = new(0.5f, 0.3f, 0.25f, 0.6f);
        private static readonly Color CenterColor = new(0.25f, 0.25f, 0.28f, 1f);

        private void Awake()
        {
            _baseRenderer = GetComponent<SpriteRenderer>();
            _baseRenderer.sprite = PrimitiveSpriteFactory.Square;
            _baseRenderer.sortingOrder = 3;
            _baseRenderer.color = CenterColor;
            transform.localScale = DefaultScale;

            var collider = GetComponent<Collider2D>();
            collider.isTrigger = true;
        }

        public void Initialize(RailNode node, RailGraph graph)
        {
            _node = node;
            _graph = graph;
            CreateSwitchVisuals();
        }

        private void CreateSwitchVisuals()
        {
            if (_node == null) return;

            // Create rail lines for each neighbor direction
            foreach (var neighbor in _node.Neighbors)
            {
                CreateRailBranch(neighbor);
            }

            // Create center pivot circle
            var pivot = new GameObject("Pivot");
            pivot.transform.SetParent(transform, false);
            var pivotSr = pivot.AddComponent<SpriteRenderer>();
            pivotSr.sprite = PrimitiveSpriteFactory.Square;
            pivotSr.sortingOrder = 6;
            pivotSr.color = new Color(0.2f, 0.2f, 0.22f, 1f);
            pivot.transform.localScale = Vector3.one * 0.5f;

            // Create highlight ring
            var ring = new GameObject("Ring");
            ring.transform.SetParent(transform, false);
            var ringSr = ring.AddComponent<SpriteRenderer>();
            ringSr.sprite = PrimitiveSpriteFactory.Square;
            ringSr.sortingOrder = 2;
            ringSr.color = new Color(0.8f, 0.7f, 0.2f, 0.4f);
            ring.transform.localScale = Vector3.one * 1.8f;
        }

        private void CreateRailBranch(RailNeighbor neighbor)
        {
            var direction = (neighbor.Node.WorldPosition - _node.WorldPosition).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Create rail line
            var railGo = new GameObject($"Rail_{neighbor.Node.Id}");
            railGo.transform.SetParent(transform, false);

            var lr = railGo.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = RailColor;
            lr.endColor = RailColor;
            lr.startWidth = 0.12f;
            lr.endWidth = 0.08f;
            lr.sortingOrder = 4;
            lr.positionCount = 2;
            lr.SetPosition(0, Vector3.zero);
            lr.SetPosition(1, direction * 1.2f);

            _railLines.Add(lr);

            // Create rail end marker
            var endGo = new GameObject($"RailEnd_{neighbor.Node.Id}");
            endGo.transform.SetParent(transform, false);
            endGo.transform.localPosition = direction * 1.0f;
            
            var endSr = endGo.AddComponent<SpriteRenderer>();
            endSr.sprite = PrimitiveSpriteFactory.Square;
            endSr.sortingOrder = 5;
            endSr.color = RailColor;
            endSr.drawMode = SpriteDrawMode.Sliced;
            endSr.size = new Vector2(0.15f, 0.15f);
            
            _railEnds.Add(endSr);
        }

        private void Update()
        {
            _animationTime += Time.deltaTime;
            
            // Pulse the active rail
            if (_activeRailHighlight != null)
            {
                var pulse = 0.8f + 0.2f * Mathf.Sin(_animationTime * 4f);
                var color = ActiveRailColor;
                color.a = pulse;
                _activeRailHighlight.startColor = color;
                _activeRailHighlight.endColor = color;
            }
        }

        public void Bind(RailSwitchState state)
        {
            _state = state;
            UpdateVisual();
        }

        public void SetVisualSize(float size)
        {
            var clamped = Mathf.Max(0.2f, size);
            _baseScale = Vector3.one * clamped;
            transform.localScale = _baseScale;
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
            var targetScale = _baseScale * 1.3f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(_baseScale, targetScale, elapsed / duration);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(targetScale, _baseScale, elapsed / duration);
                yield return null;
            }

            transform.localScale = _baseScale;
            _pulseRoutine = null;
        }

        public void UpdateVisual()
        {
            if (_state == null || _node == null || _graph == null) return;

            var targetId = _state.CurrentTarget;
            var targetNode = string.IsNullOrEmpty(targetId) ? null : _graph.GetNode(targetId);

            _activeRailHighlight = null;

            // Update each rail branch
            var neighborIndex = 0;
            foreach (var neighbor in _node.Neighbors)
            {
                if (neighborIndex >= _railLines.Count) break;

                var isActive = neighbor.Node == targetNode;
                var lr = _railLines[neighborIndex];
                var endSr = _railEnds[neighborIndex];

                if (isActive)
                {
                    // Active rail - bright and connected
                    lr.startColor = ActiveRailColor;
                    lr.endColor = ActiveRailColor;
                    lr.startWidth = 0.15f;
                    lr.endWidth = 0.12f;
                    endSr.color = ActiveRailColor;
                    endSr.size = new Vector2(0.2f, 0.2f);
                    _activeRailHighlight = lr;
                }
                else
                {
                    // Inactive rail - dimmed and "broken" appearance
                    lr.startColor = InactiveRailColor;
                    lr.endColor = new Color(InactiveRailColor.r, InactiveRailColor.g, InactiveRailColor.b, 0.2f);
                    lr.startWidth = 0.08f;
                    lr.endWidth = 0.04f;
                    endSr.color = InactiveRailColor;
                    endSr.size = new Vector2(0.1f, 0.1f);
                }

                neighborIndex++;
            }
        }

        public string NodeId => _state?.NodeId;
    }
}
