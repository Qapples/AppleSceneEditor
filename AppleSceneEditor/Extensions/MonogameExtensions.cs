using GrappleFightNET5.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Extensions
{
    public static class MonogameExtensions
    {
        public static void Draw(this ref Ray ray, ref Camera camera, GraphicsDevice graphicsDevice, Color color,
            int length = 1)
        {
            ray.Deconstruct(out Vector3 startRayPos, out Vector3 direction);
            Vector3 endRayPos = direction * length + startRayPos;

            VertexPositionColor[] vertices =
            {
                new(startRayPos, color),
                new(endRayPos, color)
            };
            short[] indices = {0, 1};

            BasicEffect effect = new(graphicsDevice)
            {
                VertexColorEnabled = true,
                World = Matrix.CreateWorld(Vector3.Zero, Vector3.Forward, Vector3.Up),
                Projection = camera.ProjectionMatrix,
                View = camera.ViewMatrix
            };

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.LineList, vertices, 0, vertices.Length, indices,
                    0, 1);
            }
            
            effect.Dispose();
        }

        public static Matrix CreateBillboad(Vector3 position, Matrix view)
        {
            Matrix result = Matrix.CreateTranslation(position);
            
            result.M11 = view.M11;
            result.M12 = view.M21;
            result.M13 = view.M31;

            result.M21 = view.M12;
            result.M22 = view.M22;
            result.M23 = view.M32;

            result.M31 = view.M13;
            result.M32 = view.M23;
            result.M33 = view.M33;

            return result;
        }
    }
}