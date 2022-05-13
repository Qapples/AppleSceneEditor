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
using GrappleFightNET5.Collision;
using GrappleFightNET5.Collision.Components;
using GrappleFightNET5.Collision.Hulls;
using GrappleFightNET5.Components;
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
    [WithEither(typeof(CollisionHullCollection), typeof(MeshData))]
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

            if (entity.Has<CollisionHullCollection>())
            {
                ref var hulls = ref entity.Get<CollisionHullCollection>();
                
                transform.Matrix.Decompose(out Vector3 _, out Quaternion rotation, out Vector3 position);

                for (int layer = 0; layer < CollisionHullPools.LayerCount; layer++)
                {
                    Color hullColor = GetLayerColor(layer);
                    
                    foreach (ICollisionHull hull in hulls.GetEnumerator(layer))
                    {
                        if (hull is ComplexBox box)
                        {
                            Matrix drawWorldMatrix = box.GetWorldMatrix(position, rotation, Vector3.One, true);

                            box.Draw(_graphicsDevice, _boxEffect, hullColor, ref drawWorldMatrix, ref worldCam, null,
                                _boxVertexBuffer);
                        }

                        bool fireRayFlag = GlobalFlag.IsFlagRaised(GlobalFlags.FireSceneEditorRay);

                        if (fireRayFlag)
                        {
                            //Handle user selection. (i.e. when the user attempts to select an entity in the scene viewer
                            float? intercept = worldCam.FireRay(hull, position, rotation, _graphicsDevice.Viewport);

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
            }
        }

        private Color GetLayerColor(int layer) => layer switch
        {
            0 => Color.DeepSkyBlue,
            1 => Color.LightSkyBlue,
            2 => Color.SeaGreen,
            3 => Color.Green,
            4 => Color.GreenYellow,
            5 => Color.Yellow,
            6 => Color.Orange,
            7 => Color.Red,
            _ => Color.White
        };

        public override void Dispose()
        {
            _boxVertexBuffer.Dispose();
            _boxEffect.Dispose();

            (_boxVertexBuffer, _boxEffect, _graphicsDevice) = (null!, null!, null!);
        }
    }
}