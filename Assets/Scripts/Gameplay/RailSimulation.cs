using System;
using System.Collections.Generic;
using System.Linq;
using RailSim.Core;
using UnityEngine;

namespace RailSim.Gameplay
{
    public class RailSimulation
    {
        private readonly RailGraph _graph;
        private readonly Dictionary<string, RailSwitchState> _switches;
        private readonly List<TrainRuntime> _trains = new();

        public IReadOnlyList<TrainRuntime> Trains => _trains;
        public IReadOnlyDictionary<string, RailSwitchState> Switches => _switches;

        public event Action<TrainRuntime, RailNode> TrainReachedGoal;
        public event Action<TrainRuntime, TrainRuntime> CollisionDetected;

        public RailSimulation(RailGraph graph, Dictionary<string, RailSwitchState> switches)
        {
            _graph = graph;
            _switches = switches;
        }

        public void SpawnTrains(IEnumerable<TrainBlueprint> blueprints)
        {
            _trains.Clear();
            foreach (var blueprint in blueprints)
            {
                if (_graph.GetNode(blueprint.startNodeId) is not { } startNode)
                {
                    Debug.LogError($"Train {blueprint.id} references missing start node.");
                    continue;
                }

                RailNode nextNode = null;
                if (!string.IsNullOrEmpty(blueprint.initialNextNodeId))
                {
                    nextNode = _graph.GetNode(blueprint.initialNextNodeId);
                }

                if (nextNode == null)
                {
                    var fallback = startNode.Neighbors.FirstOrDefault();
                    nextNode = fallback.Node;
                }

                if (nextNode == null)
                {
                    Debug.LogError($"Train {blueprint.id} has no available neighbor to depart from node {startNode.Id}.");
                    continue;
                }

                var edge = FindEdge(startNode, nextNode);
                if (edge == null)
                {
                    Debug.LogError($"Train {blueprint.id} cannot find edge from {startNode.Id} to {nextNode?.Id}.");
                    continue;
                }

                var runtime = new TrainRuntime(blueprint, startNode, nextNode, edge);

                _trains.Add(runtime);
            }
        }

        public void ToggleSwitch(string nodeId)
        {
            if (_switches.TryGetValue(nodeId, out var state))
            {
                state.Toggle();
            }
        }

        public void Update(float deltaTime, string goalNodeId)
        {
            foreach (var train in _trains)
            {
                if (train.HasFinished)
                {
                    continue;
                }

                AdvanceTrain(train, deltaTime);
                if (train.HasFinished && HasReachedGoal(train, goalNodeId))
                {
                    TrainReachedGoal?.Invoke(train, train.CurrentNode);
                }
            }

            DetectCollisions();
        }

        public void ForceCompleteTrain(TrainRuntime train, RailNode goalNodeOverride = null)
        {
            if (train == null)
            {
                return;
            }

            train.MarkFinished();
            var node = goalNodeOverride ?? train.CurrentNode ?? train.NextNode ?? train.PreviousNode;
            TrainReachedGoal?.Invoke(train, node);
        }

        private static bool HasReachedGoal(TrainRuntime train, string goalNodeId)
        {
            if (train.CurrentNode == null)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(goalNodeId) && train.CurrentNode.Id == goalNodeId)
            {
                return true;
            }

            return train.CurrentNode.Type == NodeType.Finish;
        }

        private void AdvanceTrain(TrainRuntime train, float deltaTime)
        {
            if (train.CurrentEdge == null)
            {
                return;
            }

            var remainingDistance = train.Speed * deltaTime;

            while (remainingDistance > 0f && !train.HasFinished)
            {
                var distanceLeftOnEdge = train.CurrentEdge.WorldDistance - train.DistanceAlongEdge;
                if (remainingDistance < distanceLeftOnEdge)
                {
                    train.DistanceAlongEdge += remainingDistance;
                    remainingDistance = 0f;
                    break;
                }

                remainingDistance -= distanceLeftOnEdge;
                train.ArriveAtNode(train.NextNode);

                if (train.CurrentNode.Type == NodeType.Finish)
                {
                    train.MarkFinished();
                    break;
                }

                var nextNode = ResolveNextNode(train);
                if (nextNode == null)
                {
                    train.MarkFinished(); // Dead end == failure -> treat as stop
                    break;
                }

                var edge = FindEdge(train.CurrentNode, nextNode);
                if (edge == null)
                {
                    train.MarkFinished();
                    break;
                }

                train.BeginEdge(nextNode, edge);
            }
        }

        private RailNode ResolveNextNode(TrainRuntime train)
        {
            var current = train.CurrentNode;
            if (current == null)
            {
                return null;
            }

            // Prefer explicit switch selection.
            if (_switches.TryGetValue(current.Id, out var switchState))
            {
                var targetId = switchState.CurrentTarget;
                if (!string.IsNullOrEmpty(targetId) && !string.Equals(targetId, train.PreviousNode?.Id, StringComparison.Ordinal))
                {
                    return _graph.GetNode(targetId);
                }
            }

            // Fall back to first neighbor that is not the previous node.
            foreach (var neighbor in current.Neighbors)
            {
                if (neighbor.Node == train.PreviousNode)
                {
                    continue;
                }

                return neighbor.Node;
            }

            return train.PreviousNode; // Only option is to go backwards.
        }

        private RailEdge FindEdge(RailNode from, RailNode to)
        {
            if (from == null || to == null)
            {
                return null;
            }

            foreach (var neighbor in from.Neighbors)
            {
                if (neighbor.Node == to)
                {
                    return neighbor.Edge;
                }
            }

            return null;
        }

        private void DetectCollisions()
        {
            for (var i = 0; i < _trains.Count; i++)
            {
                var a = _trains[i];
                if (a.HasFinished)
                {
                    continue;
                }

                for (var j = i + 1; j < _trains.Count; j++)
                {
                    var b = _trains[j];
                    if (b.HasFinished)
                    {
                        continue;
                    }

                    if (IsCollision(a, b))
                    {
                        CollisionDetected?.Invoke(a, b);
                        return;
                    }
                }
            }
        }

        private static bool IsCollision(TrainRuntime a, TrainRuntime b)
        {
            if (a.CurrentEdge != null && a.CurrentEdge == b.CurrentEdge)
            {
                return Mathf.Abs(a.DistanceAlongEdge - b.DistanceAlongEdge) < 0.1f;
            }

            if (a.CurrentNode != null && a.CurrentNode == b.CurrentNode)
            {
                return true;
            }

            return false;
        }
    }

    public class TrainRuntime
    {
        public TrainBlueprint Blueprint { get; }
        public RailEdge CurrentEdge { get; private set; }
        public RailNode CurrentNode { get; private set; }
        public RailNode NextNode { get; private set; }
        public RailNode PreviousNode { get; private set; }
        public float DistanceAlongEdge { get; set; }
        public float Speed => Blueprint.metersPerSecond;
        public bool HasFinished { get; private set; }

        public TrainRuntime(TrainBlueprint blueprint, RailNode startNode, RailNode nextNode, RailEdge edge)
        {
            Blueprint = blueprint;
            CurrentNode = startNode;
            DistanceAlongEdge = 0f;
            BeginEdge(nextNode, edge);
        }

        public void ArriveAtNode(RailNode node)
        {
            CurrentNode = node;
            NextNode = null;
            CurrentEdge = null;
            DistanceAlongEdge = 0f;
        }

        public void BeginEdge(RailNode target, RailEdge edge)
        {
            if (target == null || edge == null)
            {
                HasFinished = true;
                CurrentEdge = null;
                return;
            }

            if (CurrentNode != null)
            {
                PreviousNode = CurrentNode;
            }

            CurrentNode = null;
            NextNode = target;
            CurrentEdge = edge;
            DistanceAlongEdge = 0f;
        }

        public void MarkFinished() => HasFinished = true;

        public Vector3 GetWorldPosition()
        {
            if (CurrentEdge == null)
            {
                return CurrentNode?.WorldPosition ?? Vector3.zero;
            }

            var a = PreviousNode?.WorldPosition ?? CurrentEdge.A.WorldPosition;
            var b = NextNode?.WorldPosition ?? CurrentEdge.B.WorldPosition;
            var t = Mathf.Approximately(CurrentEdge.WorldDistance, 0f)
                ? 0f
                : Mathf.Clamp01(DistanceAlongEdge / CurrentEdge.WorldDistance);
            return Vector3.Lerp(a, b, t);
        }

        public Vector3 GetDirection()
        {
            if (CurrentEdge != null)
            {
                var a = PreviousNode?.WorldPosition ?? CurrentEdge.A.WorldPosition;
                var b = NextNode?.WorldPosition ?? CurrentEdge.B.WorldPosition;
                return (b - a).normalized;
            }

            if (CurrentNode != null)
            {
                if (NextNode != null)
                {
                    return (NextNode.WorldPosition - CurrentNode.WorldPosition).normalized;
                }

                if (PreviousNode != null)
                {
                    return (CurrentNode.WorldPosition - PreviousNode.WorldPosition).normalized;
                }
            }

            return Vector3.right;
        }
    }
}

