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
        public event Action<TrainRuntime> DeadEndReached;
        public event Action<TrainRuntime, RailNode> WrongSwitchEntry;
        public event Action<RailEdge, Vector3> EdgeBroken;
        public event Action<BonusRuntime, TrainRuntime> BonusCollected;

        private readonly List<BonusRuntime> _bonuses = new();
        public IReadOnlyList<BonusRuntime> Bonuses => _bonuses;

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

        public void SpawnBonuses(IEnumerable<Core.BonusBlueprint> blueprints)
        {
            _bonuses.Clear();
            foreach (var blueprint in blueprints)
            {
                var edge = _graph.GetEdge(blueprint.edgeId);
                if (edge == null)
                {
                    Debug.LogWarning($"Bonus {blueprint.id} references missing edge {blueprint.edgeId}");
                    continue;
                }
                _bonuses.Add(new BonusRuntime(blueprint, edge));
            }
        }

        private void CheckBonusCollection()
        {
            foreach (var bonus in _bonuses)
            {
                if (bonus.IsCollected) continue;

                foreach (var train in _trains)
                {
                    if (train.HasFinished) continue;
                    if (train.CurrentEdge != bonus.Edge) continue;

                    var trainPos = train.GetWorldPosition();
                    var distance = Vector3.Distance(trainPos, bonus.WorldPosition);
                    
                    if (distance < 0.5f)
                    {
                        bonus.Collect();
                        BonusCollected?.Invoke(bonus, train);
                    }
                }
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
            CheckBonusCollection();
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
                
                var arrivedEdge = train.CurrentEdge;
                var arrivalNode = train.NextNode;
                var cameFromNode = train.PreviousNode;
                
                train.ArriveAtNode(arrivalNode);

                // Check if this is a one-time-use edge and break it
                if (arrivedEdge.OneTimeUse && !arrivedEdge.IsBroken)
                {
                    arrivedEdge.MarkBroken();
                    var breakPos = arrivedEdge.IsCurved 
                        ? arrivedEdge.GetPointAtT(0.5f) 
                        : (arrivedEdge.A.WorldPosition + arrivedEdge.B.WorldPosition) * 0.5f;
                    EdgeBroken?.Invoke(arrivedEdge, breakPos);
                }

                if (train.CurrentNode.Type == NodeType.Finish)
                {
                    train.MarkFinished();
                    break;
                }

                var nextNode = ResolveNextNode(train);
                if (nextNode == null)
                {
                    train.MarkFinished();
                    DeadEndReached?.Invoke(train);
                    break;
                }

                var edge = FindEdge(train.CurrentNode, nextNode);
                if (edge == null)
                {
                    train.MarkFinished();
                    DeadEndReached?.Invoke(train);
                    break;
                }

                // Check if the edge is broken
                if (edge.IsBroken)
                {
                    train.MarkFinished();
                    DeadEndReached?.Invoke(train);
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

        /// <summary>
        /// Detects collisions between trains.
        /// A collision occurs ONLY when two different active trains physically overlap.
        /// </summary>
        private void DetectCollisions()
        {
            // Get list of active (not finished) trains
            var activeTrains = new List<TrainRuntime>();
            foreach (var train in _trains)
            {
                if (!train.HasFinished)
                {
                    activeTrains.Add(train);
                }
            }
            
            // Need at least 2 active trains for any collision to be possible
            if (activeTrains.Count < 2)
            {
                return;
            }
            
            // Check each unique pair of active trains
            for (var i = 0; i < activeTrains.Count - 1; i++)
            {
                var trainA = activeTrains[i];
                
                for (var j = i + 1; j < activeTrains.Count; j++)
                {
                    var trainB = activeTrains[j];
                    
                    if (CheckTrainCollision(trainA, trainB))
                    {
                        CollisionDetected?.Invoke(trainA, trainB);
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if two trains are colliding (physically overlapping).
        /// </summary>
        private static bool CheckTrainCollision(TrainRuntime trainA, TrainRuntime trainB)
        {
            // Ensure we have two different, valid trains
            if (trainA == null || trainB == null)
            {
                return false;
            }
            
            // Same object reference - not a collision
            if (ReferenceEquals(trainA, trainB))
            {
                return false;
            }
            
            // Same train ID - not a collision (same train can't collide with itself)
            if (string.Equals(trainA.Blueprint.id, trainB.Blueprint.id, System.StringComparison.Ordinal))
            {
                return false;
            }
            
            // Check elevation - trains on different levels can't collide
            var elevationA = trainA.CurrentEdge?.Elevation ?? 0;
            var elevationB = trainB.CurrentEdge?.Elevation ?? 0;
            if (elevationA != elevationB)
            {
                return false;
            }
            
            // Get world positions of both trains
            var positionA = trainA.GetWorldPosition();
            var positionB = trainB.GetWorldPosition();
            
            // Calculate 2D distance (ignore Z for top-down view)
            var deltaX = positionA.x - positionB.x;
            var deltaY = positionA.y - positionB.y;
            var distanceSquared = deltaX * deltaX + deltaY * deltaY;
            
            // Collision radius - trains are about 1 unit long, 0.4 units wide
            // Two trains collide when their centers are closer than this
            const float collisionThreshold = 0.3f;
            const float collisionThresholdSquared = collisionThreshold * collisionThreshold;
            
            return distanceSquared < collisionThresholdSquared;
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

            var t = Mathf.Approximately(CurrentEdge.WorldDistance, 0f)
                ? 0f
                : Mathf.Clamp01(DistanceAlongEdge / CurrentEdge.WorldDistance);

            // Handle direction: are we going from A to B or B to A?
            var goingForward = PreviousNode == CurrentEdge.A || NextNode == CurrentEdge.B;
            
            if (CurrentEdge.IsCurved)
            {
                return CurrentEdge.GetPointAtT(goingForward ? t : 1f - t);
            }

            var a = goingForward ? CurrentEdge.A.WorldPosition : CurrentEdge.B.WorldPosition;
            var b = goingForward ? CurrentEdge.B.WorldPosition : CurrentEdge.A.WorldPosition;
            return Vector3.Lerp(a, b, t);
        }

        public Vector3 GetDirection()
        {
            if (CurrentEdge != null)
            {
                var t = Mathf.Approximately(CurrentEdge.WorldDistance, 0f)
                    ? 0f
                    : Mathf.Clamp01(DistanceAlongEdge / CurrentEdge.WorldDistance);

                var goingForward = PreviousNode == CurrentEdge.A || NextNode == CurrentEdge.B;

                if (CurrentEdge.IsCurved)
                {
                    var dir = CurrentEdge.GetDirectionAtT(goingForward ? t : 1f - t);
                    return goingForward ? dir : -dir;
                }

                var a = goingForward ? CurrentEdge.A.WorldPosition : CurrentEdge.B.WorldPosition;
                var b = goingForward ? CurrentEdge.B.WorldPosition : CurrentEdge.A.WorldPosition;
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

    public class BonusRuntime
    {
        public string Id { get; }
        public RailEdge Edge { get; }
        public float PositionOnEdge { get; }
        public int StarValue { get; }
        public bool IsCollected { get; private set; }
        public Vector3 WorldPosition { get; }

        public BonusRuntime(Core.BonusBlueprint blueprint, RailEdge edge)
        {
            Id = blueprint.id;
            Edge = edge;
            PositionOnEdge = Mathf.Clamp01(blueprint.positionOnEdge);
            StarValue = Mathf.Max(1, blueprint.starValue);
            WorldPosition = edge.GetPointAtT(PositionOnEdge);
        }

        public void Collect() => IsCollected = true;
    }
}

