using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using AppleScene.Helpers;
using AppleScene.Rendering;
using AppleSceneEditor.Commands;
using AppleSceneEditor.ComponentFlags;
using AppleSceneEditor.Systems.Axis;
using DefaultEcs;
using DefaultEcs.System;
using DefaultEcs.Threading;
using GrappleFightNET5.Components;
using GrappleFightNET5.Components.Collision;
using GrappleFightNET5.Components.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AppleSceneEditor.Systems
{
    /// <summary>
    /// System responsible for drawing drawable components that entities may have.
    /// </summary>
    [With(typeof(Transform))]
    [WithEither(typeof(ComplexBox), typeof(MeshData))]
    public sealed class EntityDrawSystem : AEntitySetSystem<GameTime>
    {
        private GraphicsDevice _graphicsDevice;
        private BasicEffect _boxEffect;
        private VertexBuffer _boxVertexBuffer;

        private static readonly RasterizerState
            SolidState = new() {FillMode = FillMode.Solid, CullMode = CullMode.None};

        //there should be 36 vertices in every ComplexBox when drawing them.

        public EntityDrawSystem(World world, GraphicsDevice graphicsDevice) : this(world,
            new DefaultParallelRunner(1), graphicsDevice)
        {
        }

        public EntityDrawSystem(World world, IParallelRunner runner, GraphicsDevice graphicsDevice) : base(world, runner)
        {
            _graphicsDevice = graphicsDevice;

            _boxVertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), 48, BufferUsage.WriteOnly);
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

                transform.Matrix.Decompose(out Vector3 _, out Quaternion rotation, out Vector3 position);
                Matrix drawWorldMatrix = box.GetWorldMatrix(position, rotation, Vector3.One, true);

                box.Draw(_graphicsDevice, _boxEffect, Color.Red, ref drawWorldMatrix, ref worldCam, null,
                    _boxVertexBuffer);

                bool fireRayFlag = GlobalFlag.IsFlagRaised(GlobalFlags.FireSceneEditorRay);

                if (fireRayFlag)
                {
                    //Handle user selection. (i.e. when the user attempts to select an entity in the scene viewer
                    Viewport viewport = _graphicsDevice.Viewport;
                    float? intercept = worldCam.FireRay(ref box, ref position, ref rotation, ref viewport);

                    if (intercept is not null)
                    {
                        //raise a "selectedEntityFlag" by adding a component which let's everyone that has access to our
                        //world know that we have selected an entity.
                        World.Set(new SelectedEntityFlag(entity));
                        GlobalFlag.SetFlag(GlobalFlags.EntitySelected, true);
                    }
                }
            }
        }

        public override void Dispose()
        {
            _boxVertexBuffer.Dispose();
            _boxEffect.Dispose();

            (_boxVertexBuffer, _boxEffect, _graphicsDevice) = (null!, null!, null!);
        }
    }
}