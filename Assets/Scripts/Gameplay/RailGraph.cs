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

            var edge = new RailEdge(
                blueprint.id,
                from,
                to,
                Mathf.Max(0.1f, blueprint.lengthMultiplier));

            _edges[edge.Id] = edge;
            from.AddNeighbor(edge, to);
            to.AddNeighbor(edge, from);
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

        public RailEdge(string id, RailNode a, RailNode b, float lengthMultiplier)
        {
            Id = id;
            A = a;
            B = b;
            LengthMultiplier = lengthMultiplier;
        }

        public RailNode GetOtherNode(RailNode current) => current == A ? B : A;

        public float WorldDistance => Vector3.Distance(A.WorldPosition, B.WorldPosition) * LengthMultiplier;
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

