using RailSim.Gameplay;
using UnityEngine;

namespace RailSim.Rendering
{
    public class TrackView : MonoBehaviour
    {
        private const float DefaultWidth = 0.22f;
        private const int CurveSegments = 24;

        private SpriteRenderer _bedRenderer;
        private SpriteRenderer _renderer;
        private SpriteRenderer _railLeft;
        private SpriteRenderer _railRight;

        // For curved tracks
        private LineRenderer _bedLine;
        private LineRenderer _baseLine;
        private LineRenderer _railLeftLine;
        private LineRenderer _railRightLine;

        private RailEdge _edge;
        private bool _isCurved;
        private float _trackWidth;

        private static readonly Color BedColor = new(0.35f, 0.28f, 0.2f, 1f);
        private static readonly Color BaseColor = new(0.25f, 0.25f, 0.28f, 1f);
        private static readonly Color RailColor = new(0.5f, 0.52f, 0.55f, 1f);
        private static readonly Color BrokenColor = new(0.6f, 0.2f, 0.15f, 0.5f);

        private void Awake()
        {
            // Will be set up in Bind based on edge type
        }

        private void SetupStraightTrack()
        {
            // Track bed (gravel/wood ties)
            _bedRenderer = gameObject.AddComponent<SpriteRenderer>();
            _bedRenderer.sprite = PrimitiveSpriteFactory.Square;
            _bedRenderer.drawMode = SpriteDrawMode.Sliced;
            _bedRenderer.sortingOrder = 0;
            _bedRenderer.color = BedColor;

            // Main track base
            _renderer = CreateRailPart("TrackBase", 1, BaseColor);
            
            // Left rail (steel)
            _railLeft = CreateRailPart("RailLeft", 2, RailColor);
            
            // Right rail (steel)
            _railRight = CreateRailPart("RailRight", 2, RailColor);
        }

        private void SetupCurvedTrack()
        {
            _bedLine = CreateCurveLine("BedLine", 0, BedColor);
            _baseLine = CreateCurveLine("BaseLine", 1, BaseColor);
            _railLeftLine = CreateCurveLine("RailLeftLine", 2, RailColor);
            _railRightLine = CreateCurveLine("RailRightLine", 2, RailColor);
        }

        private LineRenderer CreateCurveLine(string name, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = order;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 4;
            return lr;
        }

        private SpriteRenderer CreateRailPart(string name, int order, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PrimitiveSpriteFactory.Square;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.sortingOrder = order;
            sr.color = color;
            return sr;
        }

        public void Bind(RailEdge edge, float width)
        {
            _edge = edge;
            _trackWidth = width <= 0f ? DefaultWidth : width;
            _isCurved = edge.IsCurved;

            if (_isCurved)
            {
                SetupCurvedTrack();
                BindCurved();
            }
            else
            {
                SetupStraightTrack();
                BindStraight();
            }
        }

        private void BindStraight()
        {
            var start = _edge.A.WorldPosition;
            var end = _edge.B.WorldPosition;
            var center = (start + end) * 0.5f;
            transform.position = center;

            var direction = end - start;
            var length = direction.magnitude;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            
            // Track bed - wider
            _bedRenderer.size = new Vector2(Mathf.Max(0.1f, length), _trackWidth * 1.6f);
            
            // Main track
            _renderer.size = new Vector2(Mathf.Max(0.1f, length), _trackWidth * 0.9f);
            
            // Rails - thin lines on edges
            var railWidth = _trackWidth * 0.12f;
            var railOffset = _trackWidth * 0.35f;
            _railLeft.size = new Vector2(length, railWidth);
            _railLeft.transform.localPosition = new Vector3(0f, railOffset, 0f);
            _railRight.size = new Vector2(length, railWidth);
            _railRight.transform.localPosition = new Vector3(0f, -railOffset, 0f);
        }

        private void BindCurved()
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;

            var positions = new Vector3[CurveSegments + 1];
            for (var i = 0; i <= CurveSegments; i++)
            {
                var t = (float)i / CurveSegments;
                positions[i] = _edge.GetPointAtT(t);
            }

            // Bed line (widest)
            SetLinePositions(_bedLine, positions, _trackWidth * 1.6f);
            
            // Base line
            SetLinePositions(_baseLine, positions, _trackWidth * 0.9f);
            
            // Rail lines with offset
            var railWidth = _trackWidth * 0.12f;
            var railOffset = _trackWidth * 0.35f;
            
            var leftPositions = new Vector3[CurveSegments + 1];
            var rightPositions = new Vector3[CurveSegments + 1];
            
            for (var i = 0; i <= CurveSegments; i++)
            {
                var t = (float)i / CurveSegments;
                var pos = _edge.GetPointAtT(t);
                var dir = _edge.GetDirectionAtT(t);
                var perpendicular = new Vector3(-dir.y, dir.x, 0f);
                
                leftPositions[i] = pos + perpendicular * railOffset;
                rightPositions[i] = pos - perpendicular * railOffset;
            }
            
            SetLinePositions(_railLeftLine, leftPositions, railWidth);
            SetLinePositions(_railRightLine, rightPositions, railWidth);
        }

        private void SetLinePositions(LineRenderer lr, Vector3[] positions, float width)
        {
            lr.positionCount = positions.Length;
            lr.SetPositions(positions);
            lr.startWidth = width;
            lr.endWidth = width;
        }

        public void MarkBroken()
        {
            if (_isCurved)
            {
                if (_bedLine != null) _bedLine.startColor = _bedLine.endColor = BrokenColor;
                if (_baseLine != null) _baseLine.startColor = _baseLine.endColor = BrokenColor;
                if (_railLeftLine != null) _railLeftLine.startColor = _railLeftLine.endColor = BrokenColor;
                if (_railRightLine != null) _railRightLine.startColor = _railRightLine.endColor = BrokenColor;
            }
            else
            {
                if (_bedRenderer != null) _bedRenderer.color = BrokenColor;
                if (_renderer != null) _renderer.color = BrokenColor;
                if (_railLeft != null) _railLeft.color = BrokenColor;
                if (_railRight != null) _railRight.color = BrokenColor;
            }
        }

        public RailEdge Edge => _edge;
    }
}

