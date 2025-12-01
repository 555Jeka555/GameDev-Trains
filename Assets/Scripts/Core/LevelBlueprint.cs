using System;
using UnityEngine;

namespace RailSim.Core
{
    [Serializable]
    public class LevelBlueprint
    {
        public string id = "Level";
        public float planningTime = 10f;
        public float simulationSpeed = 1f;
        public string goalNodeId;
        public NodeBlueprint[] nodes = Array.Empty<NodeBlueprint>();
        public EdgeBlueprint[] edges = Array.Empty<EdgeBlueprint>();
        public SwitchBlueprint[] switches = Array.Empty<SwitchBlueprint>();
        public TrainBlueprint[] trains = Array.Empty<TrainBlueprint>();
    }

    [Serializable]
    public class NodeBlueprint
    {
        public string id;
        public SerializableVector2 gridPosition;
        public string type = "Generic";

        public NodeType GetNodeType()
        {
            return type switch
            {
                "Start" => NodeType.Start,
                "Finish" => NodeType.Finish,
                _ => NodeType.Generic
            };
        }
    }

    [Serializable]
    public class EdgeBlueprint
    {
        public string id;
        public string fromNodeId;
        public string toNodeId;
        public float lengthMultiplier = 1f;
    }

    [Serializable]
    public class SwitchBlueprint
    {
        public string nodeId;
        public string[] neighborCycle = Array.Empty<string>();
        public int initialIndex;
    }

    [Serializable]
    public class TrainBlueprint
    {
        public string id = "Train";
        public TrainKind kind = TrainKind.Passenger;
        public string startNodeId;
        public string initialNextNodeId;
        public float metersPerSecond = 2f;
    }

    [Serializable]
    public struct SerializableVector2
    {
        public float x;
        public float y;

        public Vector2 ToVector2() => new(x, y);
    }

    public enum NodeType
    {
        Generic,
        Start,
        Finish
    }

    public enum TrainKind
    {
        Passenger,
        Freight
    }

    public static class LevelBlueprintLoader
    {
        public static LevelBlueprint FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("Level json is empty.");
            }

            var blueprint = JsonUtility.FromJson<LevelBlueprint>(json);
            if (blueprint == null)
            {
                throw new InvalidOperationException("Unable to parse level blueprint.");
            }

            return blueprint;
        }
    }
}

