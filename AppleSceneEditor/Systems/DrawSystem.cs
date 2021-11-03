using System;
using System.Runtime.InteropServices;
using AppleScene.Helpers;
using AppleScene.Rendering;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using GrappleFightNET5.Components.Camera;
using GrappleFightNET5.Components.Collision;
using GrappleFightNET5.Components.Events;
using GrappleFightNET5.Components.Misc;
using GrappleFightNET5.Components.Transform;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AppleSceneEditor.Systems
{
    /// <summary>
    /// System responsible for drawing 3d objects.
    /// </summary>
    [With(typeof(Transform))]
    [WithEither(typeof(ComplexBox), typeof(MeshData))]
    public sealed class DrawSystem : AEntitySetSystem<GameTime>
    {
        private readonly GraphicsDevice _graphicsDevice;
        private readonly BasicEffect _boxEffect;
        private readonly VertexBuffer _boxVertexBuffer;

        private static readonly RasterizerState
            SolidState = new() {FillMode = FillMode.Solid, CullMode = CullMode.None};

        //we have to explicitly set each field in each constructor because C#. (can't use a separate method because I
        //want the fields to be readonly)
        //there should be 36 vertices in every ComplexBox when drawing them.
        
        public DrawSystem(World world, GraphicsDevice graphicsDevice) : base(world)
        {
            _graphicsDevice = graphicsDevice;
            _boxVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 36, BufferUsage.WriteOnly);
            _boxEffect = new BasicEffect(_graphicsDevice)
                {Alpha = 1, VertexColorEnabled = true, LightingEnabled = false};
        }

        public DrawSystem(World world, IParallelRunner runner, GraphicsDevice graphicsDevice) : base(world, runner)
        {
            _graphicsDevice = graphicsDevice;
            _boxVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 36, BufferUsage.WriteOnly);
            _boxEffect = new BasicEffect(_graphicsDevice)
                {Alpha = 1, VertexColorEnabled = true, LightingEnabled = false};
        }

        protected override void Update(GameTime gameTime, in Entity entity)
        {
            //get the camera from the world. The camera can be apart of any entity, but there should be only one
            //camera per world.
            ref var worldCam = ref World.Get<Camera>();
            ref var transform = ref entity.Get<Transform>();

            if (entity.Has<MeshData>())
            {
                var meshData = entity.Get<MeshData>();

                //Note: CollectionsMarshal.AsSpan(List<T>) is unsafe and therefore the incoming list must not be edited.

                if (entity.Has<AnimationComponent>())
                {
                    ref var animComponent = ref entity.Get<AnimationComponent>();
                    animComponent.IncrementActives(gameTime.ElapsedGameTime);

                    ReadOnlySpan<ActiveAnimation> inAnimations =
                        CollectionsMarshal.AsSpan(animComponent.ActiveAnimations);
                    meshData.Draw(in transform.Matrix, worldCam.ViewMatrix, in worldCam.ProjectionMatrix,
                        in inAnimations, SolidState);

                    //update events if they have one.
                    if (entity.Has<AnimationEvents>())
                    {
                        ref var events = ref entity.Get<AnimationEvents>();

                        foreach (ActiveAnimation actAnim in animComponent.ActiveAnimations)
                        {
                            ActiveAnimationWrapper wrapper = new() {ActiveAnimation = actAnim};
                            events.Update(ref wrapper, in entity, World);
                        }
                    }

                    animComponent.CleanActives();
                }
                else
                {
                    meshData.Draw(in transform.Matrix, worldCam.ViewMatrix, in worldCam.ProjectionMatrix,
                        ReadOnlySpan<ActiveAnimation>.Empty, SolidState);
                }
            }

            if (entity.Has<ComplexBox>())
            {
                ref var box = ref entity.Get<ComplexBox>();
                box.Draw(_graphicsDevice, _boxEffect, Color.Red, ref worldCam, null, _boxVertexBuffer);
            }
        }

        public override void Dispose()
        {
            _boxVertexBuffer.Dispose();
            _boxEffect.Dispose();
        }
    }
}