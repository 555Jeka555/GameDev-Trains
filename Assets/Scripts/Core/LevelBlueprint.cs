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
        public BonusBlueprint[] bonuses = Array.Empty<BonusBlueprint>();
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
        public bool oneTimeUse = false;  // Path breaks after first use
        public bool isOneWay = false;    // Can only travel in from->to direction
        public int elevation = 0;         // Track height level (for bridges/tunnels)
        public SerializableVector2[] controlPoints;  // Bezier control points (optional)
    }

    [Serializable]
    public class BonusBlueprint
    {
        public string id;
        public string edgeId;        // Which edge the bonus is on
        public float positionOnEdge = 0.5f;  // 0-1 position along edge
        public int starValue = 1;    // How many bonus stars
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
        public string colorHex = "";  // Custom color in hex format (e.g., "FF5500")
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

