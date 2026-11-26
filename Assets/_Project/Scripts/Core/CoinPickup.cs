using System;

namespace CoinRush.Core
{
    public static class CoinPickup
    {
        /// <summary>Finds the first intersection with the pickup circle during one tick.</summary>
        public static float EntryTime(float startX, float startY, float endX, float endY, float radius)
        {
            var distance = startX * startX + startY * startY - radius * radius;
            if (distance <= 0f)
                return 0f;

            var dx = endX - startX;
            var dy = endY - startY;
            var length = dx * dx + dy * dy;
            var projection = startX * dx + startY * dy;
            var discriminant = projection * projection - length * distance;
            if (length <= 0f || projection >= 0f || discriminant < 0f)
                return float.PositiveInfinity;

            // Equivalent to the smaller quadratic root, without subtracting nearly equal values.
            var contact = distance / (-projection + MathF.Sqrt(discriminant));
            return contact <= 1f ? contact : float.PositiveInfinity;
        }
    }
}
