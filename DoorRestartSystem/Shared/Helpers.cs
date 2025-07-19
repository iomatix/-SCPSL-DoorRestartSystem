namespace DoorRestartSystem.Utilities
{
    using UnityEngine;

    /// <summary>
    /// Provides utility methods for geometric calculations within the DoorRestartSystem plugin.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// Calculates the Euclidean distance between two points in 3D space.
        /// </summary>
        /// <param name="a">The first point.</param>
        /// <param name="b">The second point.</param>
        /// <returns>The distance between the two points.</returns>
        public static float Distance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }
    }
}