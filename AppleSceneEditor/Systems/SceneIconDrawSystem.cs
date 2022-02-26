using System.Collections.Generic;
using AppleSceneEditor.Extensions;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using GrappleFightNET5.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Systems
{
    [With(typeof(Transform))]
    [WithEither(typeof(Camera))]
    public sealed class SceneIconDrawSystem : AEntitySetSystem<GameTime>
    {
        private GraphicsDevice _graphicsDevice;
        private Dictionary<string, Texture2D> _icons;

        private BasicEffect _effect;
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        
        private VertexPositionNormalTexture[] _vertices;
        private short[] _indices;

        private const float IconHalfWidth = 1f;
        private const float IconHalfHeight = 1f;

        public SceneIconDrawSystem(World world, GraphicsDevice graphicsDevice, Dictionary<string, Texture2D> icons) :
            this(world, graphicsDevice, icons, new DefaultParallelRunner(1))
        {
        }

        public SceneIconDrawSystem(World world, GraphicsDevice graphicsDevice, Dictionary<string, Texture2D> icons,
            IParallelRunner runner) : base(world, runner)
        {
            _graphicsDevice = graphicsDevice;
            _icons = icons;

            _effect = new BasicEffect(_graphicsDevice) {LightingEnabled = false, TextureEnabled = true};
            _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionNormalTexture), 4, BufferUsage.WriteOnly);
            _indexBuffer = new IndexBuffer(graphicsDevice, IndexElementSize.SixteenBits, 6, BufferUsage.WriteOnly);
            
            _vertices = new VertexPositionNormalTexture[_vertexBuffer.VertexCount];
            _indices = new short[] {0, 1, 2, 2, 1, 3}; //clockwise
            _indexBuffer.SetData(_indices);

            for (int i = 0; i < _vertices.Length; i++)
            {
                _vertices[i].Normal = Vector3.Forward;
            }
        }

        protected override void Update(GameTime state, in Entity entity)
        {
            ref var worldCam = ref World.Get<Camera>();
            ref var worldCamProp = ref World.Get<CameraProperties>();
            ref var entityTransform = ref entity.Get<Transform>();
            
            if (entity.Has<Camera>())
            {
                ref var camera = ref entity.Get<Camera>();

                Texture2D texture = _icons["camera_icon"];
                _effect.Texture = texture;

                FillVertices(_vertices);
                _vertexBuffer.SetData(_vertices);

                Matrix finalTransform =
                    MonogameExtensions.CreateBillboad(camera.LocalPosition + entityTransform.Matrix.Translation,
                        worldCam.ViewMatrix);

                DrawIcon(ref finalTransform, ref worldCam, _vertexBuffer, _effect);
            }
        }

        private void FillVertices(VertexPositionNormalTexture[] vertices)
        {
            vertices[0].Position = new Vector3(-IconHalfWidth, -IconHalfHeight, 0f); // bottom left
            vertices[1].Position = new Vector3(IconHalfWidth, -IconHalfHeight, 0f);  // bottom right
            vertices[2].Position = new Vector3(-IconHalfWidth, IconHalfHeight, 0f);  // top left
            vertices[3].Position = new Vector3(IconHalfWidth, IconHalfHeight, 0f);   // top right
            
            //(0, 0) is top left of texture
            vertices[0].TextureCoordinate = new Vector2(0f, 1f); //bottom left
            vertices[1].TextureCoordinate = new Vector2(1f, 1f); //bottom right;
            vertices[2].TextureCoordinate = new Vector2(0f, 0f); //top left
            vertices[3].TextureCoordinate = new Vector2(1f, 0f); //top right
        }

        private void DrawIcon(ref Matrix worldTransform, ref Camera worldCam, VertexBuffer buffer, BasicEffect effect)
        {
            effect.World = worldTransform;
            effect.View = worldCam.ViewMatrix;
            effect.Projection = worldCam.ProjectionMatrix;

            RasterizerState prevRasterState = _graphicsDevice.RasterizerState;
            _graphicsDevice.RasterizerState = SolidState;
            _graphicsDevice.SetVertexBuffer(buffer);
            _graphicsDevice.Indices = _indexBuffer;

            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
            }

            _graphicsDevice.RasterizerState = prevRasterState;
            _graphicsDevice.Indices = null;
        }

        public override void Dispose()
        {
            _effect.Dispose();
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();

            foreach (Texture2D texture in _icons.Values)
            {
                if (!texture.IsDisposed)
                {
                    texture.Dispose();
                }
            }

            (_graphicsDevice, _icons, _effect, _vertexBuffer, _indexBuffer, _vertices, _indices) =
                (null!, null!, null!, null!, null!, null!, null!);
        }

        private static readonly RasterizerState SolidState = new()
            {FillMode = FillMode.Solid, CullMode = CullMode.None};
    }
}