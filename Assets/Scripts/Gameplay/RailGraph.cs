using System;
using System.Collections.Generic;
using RailSim.Core;
using UnityEngine;

namespace RailSim.Gameplay
{
    public class RailGraph
    {
        private readonly Dictionary<string, RailNode> _nodes = new();
        private readonly Dictionary<string, RailEdge> _edges = new();

        public IReadOnlyDictionary<string, RailNode> Nodes => _nodes;

        public RailNode AddNode(NodeBlueprint blueprint)
        {
            var node = new RailNode(
                blueprint.id,
                blueprint.gridPosition.ToVector2(),
                blueprint.GetNodeType());
            _nodes[node.Id] = node;
            return node;
        }

        public void AddEdge(EdgeBlueprint blueprint)
        {
            if (!_nodes.TryGetValue(blueprint.fromNodeId, out var from) ||
                !_nodes.TryGetValue(blueprint.toNodeId, out var to))
            {
                Debug.LogError($"Edge {blueprint.id} references missing nodes.");
                return;
            }

            // Convert control points from grid to world coordinates
            Vector3[] worldControlPoints = null;
            if (blueprint.controlPoints != null && blueprint.controlPoints.Length > 0)
            {
                worldControlPoints = new Vector3[blueprint.controlPoints.Length];
                for (var i = 0; i < blueprint.controlPoints.Length; i++)
                {
                    worldControlPoints[i] = IsometricMath.GridToWorld(blueprint.controlPoints[i].ToVector2(), 0f);
                }
            }

            var edge = new RailEdge(
                blueprint.id,
                from,
                to,
                Mathf.Max(0.1f, blueprint.lengthMultiplier),
                blueprint.oneTimeUse,
                blueprint.isOneWay,
                blueprint.elevation,
                worldControlPoints);

            _edges[edge.Id] = edge;
            
            // For one-way edges, only add neighbor in forward direction
            from.AddNeighbor(edge, to);
            if (!blueprint.isOneWay)
            {
                to.AddNeighbor(edge, from);
            }
        }

        public RailNode GetNode(string id) => _nodes.TryGetValue(id, out var node) ? node : null;

        public RailEdge GetEdge(string id) => _edges.TryGetValue(id, out var edge) ? edge : null;
    }

    public class RailNode
    {
        private readonly List<RailNeighbor> _neighbors = new();

        public string Id { get; }
        public Vector2 GridPosition { get; }
        public Vector3 WorldPosition { get; }
        public NodeType Type { get; }
        public IReadOnlyList<RailNeighbor> Neighbors => _neighbors;

        public RailNode(string id, Vector2 gridPosition, NodeType type)
        {
            Id = id;
            GridPosition = gridPosition;
            WorldPosition = IsometricMath.GridToWorld(gridPosition, 0f);
            Type = type;
        }

        public void AddNeighbor(RailEdge edge, RailNode target)
        {
            _neighbors.Add(new RailNeighbor(edge, target));
        }
    }

    public readonly struct RailNeighbor
    {
        public readonly RailEdge Edge;
        public readonly RailNode Node;

        public RailNeighbor(RailEdge edge, RailNode node)
        {
            Edge = edge;
            Node = node;
        }
    }

    public class RailEdge
    {
        public string Id { get; }
        public RailNode A { get; }
        public RailNode B { get; }
        public float LengthMultiplier { get; }
        public bool OneTimeUse { get; }
        public bool IsOneWay { get; }
        public int Elevation { get; }
        public bool IsBroken { get; private set; }
        public Vector3[] ControlPoints { get; }
        public bool IsCurved => ControlPoints != null && ControlPoints.Length > 0;

        private float _cachedLength = -1f;

        public RailEdge(string id, RailNode a, RailNode b, float lengthMultiplier, 
            bool oneTimeUse = false, bool isOneWay = false, int elevation = 0, Vector3[] controlPoints = null)
        {
            Id = id;
            A = a;
            B = b;
            LengthMultiplier = lengthMultiplier;
            OneTimeUse = oneTimeUse;
            IsOneWay = isOneWay;
            Elevation = elevation;
            ControlPoints = controlPoints;
        }

        public RailNode GetOtherNode(RailNode current) => current == A ? B : A;

        public float WorldDistance
        {
            get
            {
                if (_cachedLength < 0f)
                {
                    _cachedLength = CalculateLength() * LengthMultiplier;
                }
                return _cachedLength;
            }
        }

        private float CalculateLength()
        {
            if (!IsCurved)
            {
                return Vector3.Distance(A.WorldPosition, B.WorldPosition);
            }

            // Approximate bezier length by sampling
            const int samples = 20;
            var length = 0f;
            var prevPos = GetPointAtT(0f);
            for (var i = 1; i <= samples; i++)
            {
                var t = (float)i / samples;
                var pos = GetPointAtT(t);
                length += Vector3.Distance(prevPos, pos);
                prevPos = pos;
            }
            return length;
        }

        public Vector3 GetPointAtT(float t)
        {
            var start = A.WorldPosition;
            var end = B.WorldPosition;

            if (!IsCurved)
            {
                return Vector3.Lerp(start, end, t);
            }

            // Cubic bezier with control points
            if (ControlPoints.Length >= 2)
            {
                return CubicBezier(start, ControlPoints[0], ControlPoints[1], end, t);
            }
            // Quadratic bezier with single control point
            if (ControlPoints.Length == 1)
            {
                return QuadraticBezier(start, ControlPoints[0], end, t);
            }

            return Vector3.Lerp(start, end, t);
        }

        public Vector3 GetDirectionAtT(float t)
        {
            const float delta = 0.01f;
            var t0 = Mathf.Max(0f, t - delta);
            var t1 = Mathf.Min(1f, t + delta);
            return (GetPointAtT(t1) - GetPointAtT(t0)).normalized;
        }

        private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * p0 +
                   2f * oneMinusT * t * p1 +
                   t * t * p2;
        }

        private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            var oneMinusT = 1f - t;
            return oneMinusT * oneMinusT * oneMinusT * p0 +
                   3f * oneMinusT * oneMinusT * t * p1 +
                   3f * oneMinusT * t * t * p2 +
                   t * t * t * p3;
        }

        public void MarkBroken()
        {
            IsBroken = true;
        }
    }

    public class RailSwitchState
    {
        private readonly string[] _cycle;
        private int _index;

        public string NodeId { get; }

        public RailSwitchState(SwitchBlueprint blueprint)
        {
            NodeId = blueprint.nodeId;
            _cycle = blueprint.neighborCycle ?? Array.Empty<string>();
            _index = Mathf.Clamp(blueprint.initialIndex, 0, Mathf.Max(0, _cycle.Length - 1));
        }

        public string CurrentTarget => _cycle.Length == 0 ? null : _cycle[_index];

        public void Toggle()
        {
            if (_cycle.Length == 0)
            {
                return;
            }

            _index = (_index + 1) % _cycle.Length;
        }
    }
}

