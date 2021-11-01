using System;
using Microsoft.Xna.Framework;

namespace AppleSceneEditor.Extensions
{
    /// <summary>
    /// Static class that provides various methods for generating vector values used in the movement system
    /// </summary>
    public static class MovementHelper
    {
        /// <summary>
        /// Generates a translation vector based on a yaw angle and pitch angle in degrees
        /// </summary>
        /// <param name="yawDegrees">Yaw angle in degrees</param>
        /// <param name="pitchDegrees">Pitch angle in degrees</param>
        /// <param name="direction">Decides how the resulting value changes in relation to the angle</param>
        /// <param name="axisLock">Locks axis and prevents them from changing in the following order if true (x-axis, y-axis, z-axis)</param>
        /// <param name="magnitude">How large the resulting vector will be</param>
        /// <returns></returns>
        public static Vector3 GenerateVectorFromDirection(float yawDegrees, float pitchDegrees,
            Direction direction, (bool xAxisLock, bool yAxisLock, bool zAxisLock) axisLock, float magnitude)
        {
            //If the direction us up or down, set the yawRadians to the radians of the yawDegrees. Else, set yawRadians to the yawDegrees minus 90
            float yawRadians = direction == Direction.Forward || direction == Direction.Backwards
                ? MathHelper.ToRadians(yawDegrees)
                : MathHelper.ToRadians(yawDegrees - 90);
            float pitchRadians = MathHelper.ToRadians(pitchDegrees);

            Vector3 movementChange =
                CalculateMovement(yawRadians, pitchRadians, axisLock) * magnitude;
            
            return direction == Direction.Backwards || direction == Direction.Left ? movementChange : -movementChange;
        }

        /// <summary>
        /// Generates a translation matrix based on a yaw angle and pitch angle in degrees
        /// </summary>
        /// <param name="yawDegrees">Yaw angle in degrees</param>
        /// <param name="pitchDegrees">Pitch angle in degrees</param>
        /// <param name="direction">Decides how the resulting value changes in relation to the angle</param>
        /// <param name="axisLock">Locks axis and prevents them from changing in the following order if true (x-axis, y-axis, z-axis)</param>
        /// <param name="magnitude">How large the resulting vector will be</param>
        /// <returns></returns>
        public static Matrix GenerateMatrixFromDirection(float yawDegrees, float pitchDegrees,
            Direction direction, (bool xAxisLock, bool yAxisLock, bool zAxisLock) axisLock, float magnitude) =>
            Matrix.CreateTranslation(GenerateVectorFromDirection(yawDegrees, pitchDegrees, direction, axisLock,
                magnitude));
        
        /// <summary>
        /// Calculate how much the velocity should change based off yaw and pitch.
        /// </summary>
        /// <param name="yawRadians">Yaw radians</param>
        /// <param name="pitchRadians">Pitch Radians</param>ss
        /// <param name="axisLock">Tells the method to not change the (x, y, z) axis</param>
        /// <returns>The velocity that adds on the velocity</returns>
        private static Vector3 CalculateMovement(float yawRadians, float pitchRadians,
            (bool xLock, bool yLock, bool zLock) axisLock) => new(
            axisLock.xLock ? 0 : (float) (Math.Sin(yawRadians) * (!axisLock.yLock ? Math.Cos(pitchRadians) : -1)),
            axisLock.yLock ? 0 : (float) -Math.Sin(pitchRadians),
            axisLock.zLock ? 0 : (float) (Math.Cos(yawRadians) * (!axisLock.yLock ? Math.Cos(pitchRadians) : -1)));
     
        public enum Direction
        {
            Forward,
            Backwards,
            Left,
            Right,
            Up,
            Down
        }
    }
}