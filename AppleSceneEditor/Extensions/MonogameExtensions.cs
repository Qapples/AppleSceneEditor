using GrappleFightNET5.Components.Camera;
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
    }
}