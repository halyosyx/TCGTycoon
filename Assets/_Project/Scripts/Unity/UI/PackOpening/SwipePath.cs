using UnityEngine;

namespace Game.Unity.UI.PackOpening
{
    /// <summary>
    /// The path a swiped card takes off the stack: thrown sideways, then falling, so it leaves along a
    /// downward curve rather than straight across. X eases out (fast first); Y drops along a power curve
    /// (late). Panel coordinates, y growing downwards. Pure, so the shape is tested without a scene.
    /// </summary>
    public static class SwipePath
    {
        /// <param name="direction">+1 to the right, −1 to the left.</param>
        /// <param name="distance">Sideways travel, in panel pixels.</param>
        /// <param name="drop">Downward travel, in panel pixels.</param>
        /// <param name="curve">Power of the drop: 1 is a straight line, higher bends it later and harder.</param>
        /// <param name="progress">0 to 1 along the swipe.</param>
        public static Vector2 Evaluate(Vector2 start, float direction, float distance, float drop, float curve, float progress)
        {
            float t = Mathf.Clamp01(progress);
            float across = 1f - (1f - t) * (1f - t);
            float down = Mathf.Pow(t, Mathf.Max(1f, curve));
            return new Vector2(start.x + Mathf.Sign(direction) * distance * across, start.y + drop * down);
        }

        /// <summary>Degrees the card has leaned into the swipe at <paramref name="progress"/>.</summary>
        public static float Tilt(float direction, float tiltDegrees, float progress)
        {
            return Mathf.Sign(direction) * tiltDegrees * Mathf.Clamp01(progress);
        }
    }
}
