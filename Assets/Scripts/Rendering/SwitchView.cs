using System.Collections;
using System.Collections.Generic;
using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    [RequireComponent(typeof(Collider2D))]
    public class SwitchView : MonoBehaviour
    {
        private static readonly Vector3 DefaultScale = Vector3.one * 0.5f;

        private RailSwitchState _state;
        private RailNode _node;
        private RailGraph _graph;
        private Coroutine _pulseRoutine;
        private Vector3 _baseScale = DefaultScale;
        private float _animationTime;

        // Rail visuals - two main rails that are always visible
        private LineRenderer _mainRailLeft;
        private LineRenderer _mainRailRight;
        
        // Moving switch rail that connects to active path
        private LineRenderer _switchRail;
        private LineRenderer _switchRailRight;
        
        // Branch indicators
        private readonly List<BranchVisual> _branches = new();
        
        // Center pivot
        private SpriteRenderer _pivotRenderer;
        private SpriteRenderer _glowRenderer;
        
        // Colors
        private static readonly Color RailSteelColor = new(0.45f, 0.47f, 0.5f, 1f);
        private static readonly Color RailBedColor = new(0.35f, 0.28f, 0.2f, 1f);
        private static readonly Color ActiveColor = new(0.3f, 0.9f, 0.4f, 1f);
        private static readonly Color InactiveColor = new(0.4f, 0.35f, 0.3f, 0.5f);
        private static readonly Color PivotColor = new(0.2f, 0.2f, 0.22f, 1f);

        private class BranchVisual
        {
            public RailNode TargetNode;
            public LineRenderer Rail;
            public LineRenderer RailBed;
            public Vector3 Direction;
        }

        private void Awake()
        {
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

            // Create glow effect
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(transform, false);
            _glowRenderer = glowGo.AddComponent<SpriteRenderer>();
            _glowRenderer.sprite = PrimitiveSpriteFactory.Square;
            _glowRenderer.sortingOrder = 1;
            _glowRenderer.color = new Color(1f, 0.9f, 0.3f, 0.2f);
            glowGo.transform.localScale = Vector3.one * 2.5f;

            // Create branch visuals for each neighbor
            foreach (var neighbor in _node.Neighbors)
            {
                CreateBranchVisual(neighbor);
            }

            // Create moving switch rail
            _switchRail = CreateRail("SwitchRail", 6, RailSteelColor, 0.18f, 0.15f);
            _switchRailRight = CreateRail("SwitchRailRight", 6, RailSteelColor, 0.18f, 0.15f);

            // Create center pivot
            var pivotGo = new GameObject("Pivot");
            pivotGo.transform.SetParent(transform, false);
            _pivotRenderer = pivotGo.AddComponent<SpriteRenderer>();
            _pivotRenderer.sprite = PrimitiveSpriteFactory.Square;
            _pivotRenderer.sortingOrder = 7;
            _pivotRenderer.color = PivotColor;
            pivotGo.transform.localScale = Vector3.one * 0.4f;

            // Create outer ring
            var ringGo = new GameObject("Ring");
            ringGo.transform.SetParent(transform, false);
            var ringSr = ringGo.AddComponent<SpriteRenderer>();
            ringSr.sprite = PrimitiveSpriteFactory.Square;
            ringSr.sortingOrder = 0;
            ringSr.color = new Color(0.3f, 0.3f, 0.35f, 1f);
            ringGo.transform.localScale = Vector3.one * 0.6f;
        }

        private void CreateBranchVisual(RailNeighbor neighbor)
        {
            var direction = (neighbor.Node.WorldPosition - _node.WorldPosition).normalized;
            
            var branch = new BranchVisual
            {
                TargetNode = neighbor.Node,
                Direction = direction
            };

            // Create rail bed (wood/gravel)
            branch.RailBed = CreateRail($"RailBed_{neighbor.Node.Id}", 3, RailBedColor, 0.35f, 0.3f);
            SetRailPositions(branch.RailBed, Vector3.zero, direction * 1.8f);

            // Create rail
            branch.Rail = CreateRail($"Rail_{neighbor.Node.Id}", 4, RailSteelColor, 0.12f, 0.1f);
            SetRailPositions(branch.Rail, Vector3.zero, direction * 1.8f);

            _branches.Add(branch);
        }

        private LineRenderer CreateRail(string name, int order, Color color, float startWidth, float endWidth)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.startWidth = startWidth;
            lr.endWidth = endWidth;
            lr.sortingOrder = order;
            lr.positionCount = 2;
            lr.numCapVertices = 4;
            
            return lr;
        }

        private void SetRailPositions(LineRenderer lr, Vector3 start, Vector3 end)
        {
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
        }

        private void Update()
        {
            _animationTime += Time.deltaTime;
            
            // Pulse glow
            if (_glowRenderer != null)
            {
                var pulse = 0.15f + 0.1f * Mathf.Sin(_animationTime * 3f);
                var color = _glowRenderer.color;
                color.a = pulse;
                _glowRenderer.color = color;
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

            // Find the active and inactive branches
            BranchVisual activeBranch = null;
            var inactiveBranches = new List<BranchVisual>();

            foreach (var branch in _branches)
            {
                if (branch.TargetNode == targetNode)
                {
                    activeBranch = branch;
                }
                else
                {
                    inactiveBranches.Add(branch);
                }
            }

            // Update branch visuals
            foreach (var branch in _branches)
            {
                var isActive = branch == activeBranch;
                
                // Rail bed color
                branch.RailBed.startColor = isActive ? RailBedColor : new Color(RailBedColor.r, RailBedColor.g, RailBedColor.b, 0.4f);
                branch.RailBed.endColor = isActive ? RailBedColor : new Color(RailBedColor.r, RailBedColor.g, RailBedColor.b, 0.2f);
                
                // Rail color - active is bright, inactive is dimmed
                branch.Rail.startColor = isActive ? ActiveColor : InactiveColor;
                branch.Rail.endColor = isActive ? ActiveColor : new Color(InactiveColor.r, InactiveColor.g, InactiveColor.b, 0.2f);
                
                // Width adjustment
                branch.Rail.startWidth = isActive ? 0.15f : 0.08f;
                branch.Rail.endWidth = isActive ? 0.12f : 0.04f;
            }

            // Position the switch rail to connect to active branch
            if (activeBranch != null && _switchRail != null)
            {
                var dir = activeBranch.Direction;
                var perpendicular = new Vector3(-dir.y, dir.x, 0f) * 0.08f;
                
                // Left switch rail
                SetRailPositions(_switchRail, perpendicular * 0.5f, dir * 0.9f + perpendicular);
                _switchRail.startColor = ActiveColor;
                _switchRail.endColor = ActiveColor;
                
                // Right switch rail
                SetRailPositions(_switchRailRight, -perpendicular * 0.5f, dir * 0.9f - perpendicular);
                _switchRailRight.startColor = ActiveColor;
                _switchRailRight.endColor = ActiveColor;
            }
            
            // Update pivot color based on state
            if (_pivotRenderer != null)
            {
                _pivotRenderer.color = activeBranch != null 
                    ? new Color(0.25f, 0.5f, 0.3f, 1f) 
                    : PivotColor;
            }
        }

        public string NodeId => _state?.NodeId;
    }
}
