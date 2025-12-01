using UnityEngine;

namespace RailSim.Core
{
    /// <summary>
    /// Utility helpers for projecting grid coordinates into isometric 2D space.
    /// </summary>
    public static class IsometricMath
    {
        private const float TileWidth = 1.0f;
        private const float TileHeight = 0.5f;

        /// <summary>
        /// Converts an integer grid coordinate into a world-space position aligned with an isometric view.
        /// </summary>
        public static Vector3 GridToWorld(Vector2 gridPosition, float z = 0f)
        {
            var x = (gridPosition.x - gridPosition.y) * (TileWidth * 0.5f);
            var y = (gridPosition.x + gridPosition.y) * (TileHeight * 0.5f);
            return new Vector3(x, y, z);
        }

        /// <summary>
        /// Calculates the signed angle between two grid points in world space for aligning sprites or lines.
        /// </summary>
        public static float AngleBetween(Vector2 from, Vector2 to)
        {
            var worldFrom = GridToWorld(from);
            var worldTo = GridToWorld(to);
            var diff = worldTo - worldFrom;
            return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        }
    }
}

