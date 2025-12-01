using UnityEngine;

namespace RailSim.Core
{
    /// <summary>
    /// Utility helpers for projecting grid coordinates into isometric 2D space.
    /// Rotated 90° left so tracks go bottom-to-top on mobile screens.
    /// </summary>
    public static class IsometricMath
    {
        private const float TileWidth = 1.0f;
        private const float TileHeight = 0.5f;

        /// <summary>
        /// Converts an integer grid coordinate into a world-space position aligned with an isometric view.
        /// Rotated 90° counter-clockwise for vertical mobile layout.
        /// </summary>
        public static Vector3 GridToWorld(Vector2 gridPosition, float z = 0f)
        {
            // Original isometric: x = (gx - gy) * 0.5, y = (gx + gy) * 0.25
            // Rotated 90° left: swap and negate to rotate counter-clockwise
            var originalX = (gridPosition.x - gridPosition.y) * (TileWidth * 0.5f);
            var originalY = (gridPosition.x + gridPosition.y) * (TileHeight * 0.5f);
            
            // Rotate 90° counter-clockwise: (x, y) -> (-y, x)
            var x = -originalY;
            var y = originalX;
            
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

